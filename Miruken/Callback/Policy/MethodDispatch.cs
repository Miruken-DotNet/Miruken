namespace Miruken.Callback.Policy
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    public class MethodDispatch
    {
        private Delegate _delegate;
        private DispatchType _dispatchType;
        private Tuple<int, int>[] _mapping;

        private enum DispatchType
        {
            LateBound,
            OpenGeneric,
            FastNoArgsVoid,
            FastOneArgVoid,
            FastTwoArgsVoid,
            FastThreeArgsVoid,
            FastFourArgsVoid,
            FastNoArgsReturn,
            FastOneArgReturn,
            FastTwoArgsReturn,
            FastThreeArgsReturn,
            FastFourArgsReturn
        }

        public MethodDispatch(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var parameters = method.GetParameters();
            ConfigureMethod(method, parameters);
            ArgumentCount = parameters.Length;
            Method        = method;
        }

        public MethodInfo Method        { get; }
        public int        ArgumentCount { get; }
        public Type       ReturnType => Method.ReturnType;
        public bool       IsVoid     => ReturnType == typeof(void);

        public object Invoke(object target, object[] args, Type returnType = null)
        {
            switch (_dispatchType)
            {
                #region Fast Invocation
                case DispatchType.FastNoArgsVoid:
                    AssertArgsCount(0, args);
                    ((NoArgsDelegate)_delegate)(target);
                    return null;
                case DispatchType.FastOneArgVoid:
                    AssertArgsCount(1, args);
                    ((OneArgDelegate)_delegate)(target, args[0]);
                    return null;
                case DispatchType.FastTwoArgsVoid:
                    AssertArgsCount(2, args);
                    ((TwoArgsDelegate)_delegate)(target, args[0], args[1]);
                    return null;
                case DispatchType.FastThreeArgsVoid:
                    AssertArgsCount(3, args);
                    ((ThreeArgsDelegate)_delegate)(target, args[0], args[1], args[2]);
                    return null;
                case DispatchType.FastFourArgsVoid:
                    AssertArgsCount(4, args);
                    ((FourArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3]);
                    return null;
                case DispatchType.FastNoArgsReturn:
                    AssertArgsCount(0, args);
                    return ((NoArgsReturnDelegate)_delegate)(target);
                case DispatchType.FastOneArgReturn:
                    AssertArgsCount(1, args);
                    return ((OneArgReturnDelegate)_delegate)(target, args[0]);
                case DispatchType.FastTwoArgsReturn:
                    AssertArgsCount(2, args);
                    return ((TwoArgsReturnDelegate)_delegate)(target, args[0], args[1]);
                case DispatchType.FastThreeArgsReturn:
                    AssertArgsCount(3, args);
                    return ((ThreeArgsReturnDelegate)_delegate)(target, args[0], args[1], args[2]);
                case DispatchType.FastFourArgsReturn:
                    AssertArgsCount(4, args);
                    ((FourArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3]);
                    return null;
                #endregion
                default:
                    return InvokeLate(target, args, returnType);
            }
        }

        protected object InvokeLate(object target, object[] args, Type returnType = null)
        {
            var method = Method;
            if (ArgumentCount > (args?.Length ?? 0))
                throw new ArgumentException($"Method {GetDescription()} expects {ArgumentCount} arguments");
            if (_mapping != null)
            {
                var argTypes = _mapping.Select(mapping =>
                {
                    if (mapping.Item1 < 0)  // return type
                    {
                        if (returnType == null)
                            throw new ArgumentException(
                                "Return type is unknown and cannot infer types");
                        return returnType.GetGenericArguments()[mapping.Item2];
                    }
                    var arg = args?[mapping.Item1];
                    if (arg == null)
                        throw new ArgumentException($"Argument {mapping.Item1} is null and cannot infer types");
                    return arg.GetType().GetGenericArguments()[mapping.Item2];
                }).ToArray();
                method = method.MakeGenericMethod(argTypes);
            }
            return method.Invoke(target, HandlerDescriptor.Binding, null, args,
                                 CultureInfo.InvariantCulture);
        }

        private void ConfigureMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            if (!method.IsGenericMethodDefinition)
            {
                var isVoid = method.ReturnType == typeof(void);
                switch (parameters.Length)
                {
                    #region Early Bound
                    case 0:
                        if (isVoid)
                        {
                            _delegate = RuntimeHelper.CreateActionNoArgs(method);
                            _dispatchType = DispatchType.FastNoArgsVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncNoArgs(method);
                            _dispatchType = DispatchType.FastNoArgsReturn;
                        }
                        return;
                    case 1:
                        if (isVoid)
                        {
                            _delegate = RuntimeHelper.CreateActionOneArg(method);
                            _dispatchType = DispatchType.FastOneArgVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncOneArg(method);
                            _dispatchType = DispatchType.FastOneArgReturn;
                        }
                        return;
                    case 2:
                        if (isVoid)
                        {
                            _delegate = RuntimeHelper.CreateActionTwoArgs(method);
                            _dispatchType = DispatchType.FastTwoArgsVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncTwoArgs(method);
                            _dispatchType = DispatchType.FastTwoArgsReturn;
                        }
                        return;
                    case 3:
                        if (isVoid)
                        {
                            _delegate = RuntimeHelper.CreateActionThreeArgs(method);
                            _dispatchType = DispatchType.FastThreeArgsVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncThreeArgs(method);
                            _dispatchType = DispatchType.FastThreeArgsReturn;
                        }
                        return;
                    case 4:
                        if (isVoid)
                        {
                            _delegate = RuntimeHelper.CreateActionFourArgs(method);
                            _dispatchType = DispatchType.FastFourArgsVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncFourArgs(method);
                            _dispatchType = DispatchType.FastFourArgsReturn;
                        }
                        return;
                    #endregion
                    default:
                        _dispatchType = DispatchType.LateBound;
                        return;
                }
            }

            var argSources = parameters
                .Where(p => p.ParameterType.ContainsGenericParameters)
                .Select(p => Tuple.Create(p.Position, p.ParameterType))
                .ToList();
            var returnType = method.ReturnType;
            if (returnType.ContainsGenericParameters)
                argSources.Add(Tuple.Create(-1, returnType));
            var methodArgs  = method.GetGenericArguments();
            var typeMapping = new Tuple<int, int>[methodArgs.Length];
            foreach (var source in argSources)
            {
                var typeArgs = source.Item2.GetGenericArguments();
                for (var i = 0; i < methodArgs.Length; ++i)
                {
                    if (typeMapping[i] != null) continue;
                    var index = Array.IndexOf(typeArgs, methodArgs[i]);
                    if (index >= 0)
                        typeMapping[i] = Tuple.Create(source.Item1, index);
                }
            }
            if (typeMapping.Contains(null))
                throw new InvalidOperationException(
                    $"Type mapping for {GetDescription()} could not be inferred");

            _mapping     = typeMapping;
            _dispatchType = DispatchType.OpenGeneric;
        }

        protected string GetDescription()
        {
            return $"{Method.ReflectedType?.FullName}:{Method.Name}";
        }

        private static void AssertArgsCount(int expected, params object[] args)
        {
            if (args.Length != expected)
                throw new ArgumentException(
                    $"Expected {expected} arguments, but {args.Length} provided");
        }
    }
}
