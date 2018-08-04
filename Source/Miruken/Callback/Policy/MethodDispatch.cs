namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Concurrency;
    using Infrastructure;

    public class MethodDispatch : MemberDispatch
    {
        private Delegate _delegate;
        private GenericMapping _mapping;
        private ConcurrentDictionary<MethodInfo, MethodDispatch> _closed;

        public MethodDispatch(
            MethodInfo method, Attribute[] attributes = null)
            : base(method, method.ReturnType, attributes)
        {
            ConfigureMethod(method);
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
            switch (DispatchType & (DispatchTypeEnum.Fast | DispatchTypeEnum.Void))
            {
                #region Fast Invocation
                case DispatchTypeEnum.FastNoArgs | DispatchTypeEnum.Void:
                    AssertArgsCount(0, args);
                    ((NoArgsDelegate)_delegate)(target);
                    return null;
                case DispatchTypeEnum.FastOneArg | DispatchTypeEnum.Void:
                    AssertArgsCount(1, args);
                    ((OneArgDelegate)_delegate)(target, args[0]);
                    return null;
                case DispatchTypeEnum.FastTwoArgs | DispatchTypeEnum.Void:
                    AssertArgsCount(2, args);
                    ((TwoArgsDelegate)_delegate)(target, args[0], args[1]);
                    return null;
                case DispatchTypeEnum.FastThreeArgs | DispatchTypeEnum.Void:
                    AssertArgsCount(3, args);
                    ((ThreeArgsDelegate)_delegate)(target, args[0], args[1], args[2]);
                    return null;
                case DispatchTypeEnum.FastFourArgs | DispatchTypeEnum.Void:
                    AssertArgsCount(4, args);
                    ((FourArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3]);
                    return null;
                case DispatchTypeEnum.FastFiveArgs | DispatchTypeEnum.Void:
                    AssertArgsCount(5, args);
                    ((FiveArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3], args[4]);
                    return null;
                case DispatchTypeEnum.FastSixArgs | DispatchTypeEnum.Void:
                    AssertArgsCount(6, args);
                    ((SixArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3], args[4], args[5]);
                    return null;
                case DispatchTypeEnum.FastSevenArgs | DispatchTypeEnum.Void:
                    AssertArgsCount(7, args);
                    ((SevenArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                    return null;
                case DispatchTypeEnum.FastNoArgs:
                    AssertArgsCount(0, args);
                    return ((FuncNoArgsDelegate)_delegate)(target);
                case DispatchTypeEnum.FastOneArg:
                    AssertArgsCount(1, args);
                    return ((FuncOneArgDelegate)_delegate)(target, args[0]);
                case DispatchTypeEnum.FastTwoArgs:
                    AssertArgsCount(2, args);
                    return ((FuncTwoArgsDelegate)_delegate)(target, args[0], args[1]);
                case DispatchTypeEnum.FastThreeArgs:
                    AssertArgsCount(3, args);
                    return ((FuncThreeArgsDelegate)_delegate)(target, args[0], args[1], args[2]);
                case DispatchTypeEnum.FastFourArgs:
                    AssertArgsCount(4, args);
                    return ((FuncFourArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3]);
                case DispatchTypeEnum.FastFiveArgs:
                    AssertArgsCount(5, args);
                    return ((FuncFiveArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3], args[4]);
                case DispatchTypeEnum.FastSixArgs:
                    AssertArgsCount(6, args);
                    return ((FuncSixArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3], args[4], args[5]);
                case DispatchTypeEnum.FastSevenArgs:
                    AssertArgsCount(7, args);
                    return ((FuncSevenArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                #endregion
                default:
                    return DispatchLate(target, args, returnType);
            }
        }

        public override MemberDispatch CloseDispatch(
            object[] args, Type returnType = null)
        {
            if (_mapping == null) return this;
            var closedMethod = ClosedMethod(args, returnType);
            return _closed.GetOrAdd(closedMethod,
                m => new MethodDispatch(m, Attributes));
        }

        protected object DispatchLate(object target, object[] args, Type returnType = null)
        {
            var method = Method;
            if (Arguments.Length > (args?.Length ?? 0))
                throw new ArgumentException(
                    $"Method {Method.GetDescription()} expects {Arguments.Length} arguments");
            if (_mapping != null)
                method = ClosedMethod(args, returnType);
            return method.Invoke(target, Binding, null, args, CultureInfo.InvariantCulture);
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

        private void ConfigureMethod(MethodInfo method)
        {
            var returnType = method.ReturnType;
            var isVoid     = returnType == typeof(void);

            var arguments = Arguments;
            if (!method.ContainsGenericParameters)
            {
                switch (arguments.Length)
                {
                    #region Early Bound
                    case 0:
                        _delegate  = isVoid
                            ? (Delegate)RuntimeHelper.CreateCall<NoArgsDelegate>(method)
                            : RuntimeHelper.CreateCall<FuncNoArgsDelegate>(method);
                        DispatchType |= DispatchTypeEnum.FastNoArgs;
                        return;
                    case 1:
                        _delegate = isVoid
                            ? (Delegate)RuntimeHelper.CreateCall<OneArgDelegate>(method)
                            : RuntimeHelper.CreateCall<FuncOneArgDelegate>(method);
                        DispatchType |= DispatchTypeEnum.FastOneArg;
                        return;
                    case 2:
                        _delegate = isVoid
                            ? (Delegate)RuntimeHelper.CreateCall<TwoArgsDelegate>(method)
                            : RuntimeHelper.CreateCall<FuncTwoArgsDelegate>(method);
                        DispatchType |= DispatchTypeEnum.FastTwoArgs;
                        return;
                    case 3:
                        _delegate = isVoid
                            ? (Delegate)RuntimeHelper.CreateCall<ThreeArgsDelegate>(method)
                            : RuntimeHelper.CreateCall<FuncThreeArgsDelegate>(method);
                        DispatchType |= DispatchTypeEnum.FastThreeArgs;
                        return;
                    case 4:
                        _delegate = isVoid
                            ? (Delegate)RuntimeHelper.CreateCall<FourArgsDelegate>(method)
                            : RuntimeHelper.CreateCall<FuncFourArgsDelegate>(method);
                        DispatchType |= DispatchTypeEnum.FastFourArgs;
                        return;
                    case 5:
                        _delegate = isVoid
                            ? (Delegate)RuntimeHelper.CreateCall<FiveArgsDelegate>(method)
                            : RuntimeHelper.CreateCall<FuncFiveArgsDelegate>(method);
                        DispatchType |= DispatchTypeEnum.FastFiveArgs;
                        return;
                    case 6:
                        _delegate = isVoid
                            ? (Delegate)RuntimeHelper.CreateCall<SixArgsDelegate>(method)
                            : RuntimeHelper.CreateCall<FuncSixArgsDelegate>(method);
                        DispatchType |= DispatchTypeEnum.FastSixArgs;
                        return;
                    case 7:
                        _delegate = isVoid
                            ? (Delegate)RuntimeHelper.CreateCall<SevenArgsDelegate>(method)
                            : RuntimeHelper.CreateCall<FuncSevenArgsDelegate>(method);
                        DispatchType |= DispatchTypeEnum.FastSevenArgs;
                        return;

                    #endregion
                    default:
                        DispatchType |= DispatchTypeEnum.LateBound;
                        return;
                }
            }

            var methodArgs = method.GetGenericArguments();
            if (returnType.ContainsGenericParameters && IsAsync)
                returnType = returnType.GenericTypeArguments[0];

            _mapping = new GenericMapping(methodArgs, arguments, returnType);
            if (!_mapping.Complete)
                throw new InvalidOperationException(
                    $"Type mapping for {method.GetDescription()} could not be inferred");

            DispatchType |= DispatchTypeEnum.LateBound;
            _closed  = new ConcurrentDictionary<MethodInfo, MethodDispatch>();
        }
    }
}
