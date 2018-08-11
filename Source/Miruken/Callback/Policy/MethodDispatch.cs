namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Concurrency;
    using Infrastructure;

    public class MethodDispatch : MemberDispatch
    {
        private Delegate _delegate;
        private GenericMapping _mapping;
        private ConcurrentDictionary<MethodInfo, MethodDispatch> _closed;
        private bool _initialized;

        public MethodDispatch(
            MethodInfo method, Attribute[] attributes = null)
            : base(method, method.ReturnType, attributes)
        {
            ConfigureMethod();
        }

        public MethodInfo Method => (MethodInfo)Member;

        public override object Invoke(
            object target, object[] args, Type returnType = null)
        {
            if (_mapping != null)
                throw new InvalidOperationException(
                    "Only closed methods can be invoked");

            if (!IsPromise)
                return Dispatch(target, args, returnType);

            try
            {
                return Dispatch(target, args, returnType);
            }
            catch (Exception exception)
            {
                if (exception is TargetException tie)
                    exception = tie.InnerException;
                return Promise.Rejected(exception).Coerce(ReturnType);
            }
        }

        private object Dispatch(object target, object[] args, Type returnType)
        {
            if (IsLateBound)
            {
                var method = _mapping != null
                           ? ClosedMethod(args, returnType)
                           : Method;
                return method.Invoke(
                    target, Binding, null, args, CultureInfo.InvariantCulture);
            }

            if (!_initialized)
            {
                object guard = this;
                LazyInitializer.EnsureInitialized(
                    ref _delegate, ref _initialized, ref guard, CreateDelegate);
            }

            var delegateFlags = DispatchType & DispatchTypeEnum.DelegateMask;
            return MemberDelegates[delegateFlags].Item2(_delegate, target, args);
        }

        public override MemberDispatch CloseDispatch(
            object[] args, Type returnType = null)
        {
            if (_mapping == null) return this;
            var closedMethod = ClosedMethod(args, returnType);
            return _closed.GetOrAdd(closedMethod,
                m => new MethodDispatch(m, Attributes));
        }

        private MethodInfo ClosedMethod(object[] args, Type returnType)
        {
            var types = args.Select((arg, index) =>
            {
                var type = arg.GetType();
                if (type.IsGenericType) return type;
                var paramType = Arguments[index].ParameterType;
                if (!paramType.IsGenericParameter &&
                    paramType.ContainsGenericParameters)
                    type = type.GetOpenTypeConformance(
                        paramType.GetGenericTypeDefinition());
                return type;
            }).ToArray();
            var argTypes = _mapping.MapTypes(types, returnType);
            return Method.MakeGenericMethod(argTypes);
        }

        private void ConfigureMethod()
        {
            var method = Method;
            if (!method.ContainsGenericParameters) return;

            var returnType = method.ReturnType;
            var methodArgs = method.GetGenericArguments();
            if (returnType.ContainsGenericParameters && IsAsync)
                returnType = returnType.GenericTypeArguments[0];

            _mapping = new GenericMapping(methodArgs, Arguments, returnType);
            if (!_mapping.Complete)
                throw new InvalidOperationException(
                    $"Type mapping for {method.GetDescription()} could not be inferred");

            _closed  = new ConcurrentDictionary<MethodInfo, MethodDispatch>();
        }

        private Delegate CreateDelegate()
        {
            var delegateFlags = DispatchType & DispatchTypeEnum.DelegateMask;
            return MemberDelegates.TryGetValue(delegateFlags, out var invoker)
                 ? RuntimeHelper.CompileMethod(Method, invoker.Item1)
                     : throw new InvalidOperationException(
                           $"Unable to create delegate for method {Method}");
        }
    }
}
