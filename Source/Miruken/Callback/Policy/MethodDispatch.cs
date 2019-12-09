namespace Miruken.Callback.Policy
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using Concurrency;
    using Infrastructure;

    public class MethodDispatch : MemberDispatch
    {
        private bool _initialized;
        private Delegate _delegate;

        public MethodDispatch(
            MethodInfo method, Attribute[] attributes = null)
            : base(method, method.ReturnType, attributes)
        {
        }

        public MethodInfo Method => (MethodInfo)Member;

        public override object Invoke(object target, object[] args, Type returnType = null)
        {
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
                return Method.Invoke(
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
