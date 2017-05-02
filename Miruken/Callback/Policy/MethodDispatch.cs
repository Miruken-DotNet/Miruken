namespace Miruken.Callback.Policy
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;

    public class MethodDispatch
    {
        private Delegate _delegate;
        private DispatchType _dispatchType;
        private Tuple<int, int>[] _mapping;

        [Flags]
        private enum DispatchType
        {
            FastNoArgs     = (1 << 0),
            FastOneArg     = (1 << 1),
            FastTwoArgs    = (1 << 2),
            FastThreeArgs  = (1 << 3),
            FastFourArgs   = (1 << 4),
            FastFiveArgs   = (1 << 5),
            Promise        = (1 << 6),
            Task           = (1 << 7),
            Void           = (1 << 8),
            LateBound      = (1 << 9),
            Fast           = FastNoArgs   | FastOneArg
                           | FastTwoArgs  | FastThreeArgs
                           | FastFourArgs | FastFiveArgs
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

        private MethodDispatch(MethodInfo method, DispatchType dispatchType)
        {
            Method        = method;
            ArgumentCount = method.GetParameters().Length;
            _dispatchType = dispatchType;
        }

        public MethodInfo Method            { get; }
        public int        ArgumentCount     { get; }
        public Type       LogicalReturnType { get; private set; }

        public Type ReturnType => Method.ReturnType;
        public bool IsVoid     => (_dispatchType & DispatchType.Void) > 0;
        public bool IsPromise  => (_dispatchType & DispatchType.Promise) > 0;
        public bool IsTask     => (_dispatchType & DispatchType.Task) > 0;
        public bool IsAsync    => IsPromise || IsTask;

        public MethodDispatch CloseMethod(object[] args, Type returnType = null)
        {
            return _mapping == null ? this 
                 : new MethodDispatch(GetClosedMethod(args, returnType),
                    (_dispatchType & ~DispatchType.Fast) | DispatchType.LateBound);
        }

        public object Invoke(object target, object[] args, Type returnType = null)
        {
            switch (_dispatchType & (DispatchType.Fast | DispatchType.Void))
            {
                #region Fast Invocation
                case DispatchType.FastNoArgs | DispatchType.Void:
                    AssertArgsCount(0, args);
                    ((NoArgsDelegate)_delegate)(target);
                    return null;
                case DispatchType.FastOneArg | DispatchType.Void:
                    AssertArgsCount(1, args);
                    ((OneArgDelegate)_delegate)(target, args[0]);
                    return null;
                case DispatchType.FastTwoArgs | DispatchType.Void:
                    AssertArgsCount(2, args);
                    ((TwoArgsDelegate)_delegate)(target, args[0], args[1]);
                    return null;
                case DispatchType.FastThreeArgs | DispatchType.Void:
                    AssertArgsCount(3, args);
                    ((ThreeArgsDelegate)_delegate)(target, args[0], args[1], args[2]);
                    return null;
                case DispatchType.FastFourArgs | DispatchType.Void:
                    AssertArgsCount(4, args);
                    ((FourArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3]);
                    return null;
                case DispatchType.FastFiveArgs | DispatchType.Void:
                    AssertArgsCount(5, args);
                    ((FiveArgsDelegate)_delegate)(target, args[0], args[1], args[2], args[3], args[4]);
                    return null;
                case DispatchType.FastNoArgs:
                    AssertArgsCount(0, args);
                    return ((NoArgsReturnDelegate)_delegate)(target);
                case DispatchType.FastOneArg:
                    AssertArgsCount(1, args);
                    return ((OneArgReturnDelegate)_delegate)(target, args[0]);
                case DispatchType.FastTwoArgs:
                    AssertArgsCount(2, args);
                    return ((TwoArgsReturnDelegate)_delegate)(target, args[0], args[1]);
                case DispatchType.FastThreeArgs:
                    AssertArgsCount(3, args);
                    return ((ThreeArgsReturnDelegate)_delegate)(target, args[0], args[1], args[2]);
                case DispatchType.FastFourArgs:
                    AssertArgsCount(4, args);
                    return ((FourArgsReturnDelegate)_delegate)(target, args[0], args[1], args[2], args[3]);
                case DispatchType.FastFiveArgs:
                    AssertArgsCount(5, args);
                    return ((FiveArgsReturnDelegate)_delegate)(target, args[0], args[1], args[2], args[3], args[4]);
                #endregion
                default:
                    return InvokeLate(target, args, returnType);
            }
        }

        protected object InvokeLate(object target, object[] args, Type returnType = null)
        {
            var method = Method;
            if (ArgumentCount > (args?.Length ?? 0))
                throw new ArgumentException($"Method {GetDescription(Method)} expects {ArgumentCount} arguments");
            if (_mapping != null)
                method = GetClosedMethod(args, returnType);
            return method.Invoke(target, Binding, null, args, CultureInfo.InvariantCulture);
        }

        private MethodInfo GetClosedMethod(object[] args, Type returnType)
        {
            var argTypes = _mapping.Select(mapping =>
            {
                if (mapping.Item1 < 0)  // use return type
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
           return Method.MakeGenericMethod(argTypes);
        }

        private void ConfigureMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            var returnType = method.ReturnType;
            var isVoid     = returnType == typeof(void);

            if (isVoid)
            {
                _dispatchType |= DispatchType.Void;
                LogicalReturnType = returnType;
            }
            else if (typeof(Promise).IsAssignableFrom(returnType))
            {
                _dispatchType |= DispatchType.Promise;
                var promise = returnType.GetOpenTypeConformance(typeof(Promise<>));
                LogicalReturnType = promise != null
                    ? promise.GetGenericArguments()[0]
                    : typeof(object);
            }
            else if (typeof(Task).IsAssignableFrom(returnType))
            {
                _dispatchType |= DispatchType.Task;
                var task = returnType.GetOpenTypeConformance(typeof(Task<>));
                LogicalReturnType = task != null
                    ? task.GetGenericArguments()[0]
                    : typeof(object);
            }
            else
                LogicalReturnType = returnType;

            if (!method.IsGenericMethodDefinition)
            {
                switch (parameters.Length)
                {
                    #region Early Bound
                    case 0:
                        _delegate  = isVoid
                                   ? (Delegate)RuntimeHelper.CreateActionNoArgs(method)
                                   : RuntimeHelper.CreateFuncNoArgs(method);
                        _dispatchType |= DispatchType.FastNoArgs;
                        return;
                    case 1:
                        _delegate = isVoid
                                  ? (Delegate)RuntimeHelper.CreateActionOneArg(method)
                                  : RuntimeHelper.CreateFuncOneArg(method);
                        _dispatchType |= DispatchType.FastOneArg;
                        return;
                    case 2:
                        _delegate = isVoid
                                  ? (Delegate)RuntimeHelper.CreateActionTwoArgs(method)
                                  : RuntimeHelper.CreateFuncTwoArgs(method);
                        _dispatchType |= DispatchType.FastTwoArgs;
                        return;
                    case 3:
                        _delegate = isVoid
                                  ? (Delegate)RuntimeHelper.CreateActionThreeArgs(method)
                                  : RuntimeHelper.CreateFuncThreeArgs(method);
                        _dispatchType |= DispatchType.FastThreeArgs;
                        return;
                    case 4:
                        _delegate = isVoid
                                  ? (Delegate)RuntimeHelper.CreateActionFourArgs(method)
                                  : RuntimeHelper.CreateFuncFourArgs(method);
                        _dispatchType |= DispatchType.FastFourArgs;
                        return;
                    case 5:
                        _delegate = isVoid
                                  ? (Delegate)RuntimeHelper.CreateActionFiveArgs(method)
                                  : RuntimeHelper.CreateFuncFiveArgs(method);
                        _dispatchType |= DispatchType.FastFiveArgs;
                        return;
                    #endregion
                    default:
                        _dispatchType |= DispatchType.LateBound;
                        return;
                }
            }

            var argSources = parameters
                .Where(p => p.ParameterType.ContainsGenericParameters)
                .Select(p => Tuple.Create(p.Position, p.ParameterType))
                .ToList();
            if (returnType.ContainsGenericParameters)
            {
                if (IsAsync)
                    returnType = returnType.GenericTypeArguments[0];
                argSources.Add(Tuple.Create(-1, returnType));
            }
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
                    $"Type mapping for {GetDescription(method)} could not be inferred");

            _dispatchType |= DispatchType.LateBound;
            _mapping = typeMapping;
        }

        private static void AssertArgsCount(int expected, params object[] args)
        {
            if (args.Length != expected)
                throw new ArgumentException(
                    $"Expected {expected} arguments, but {args.Length} provided");
        }

        private static string GetDescription(MethodInfo method)
        {
            return $"{method.ReflectedType?.FullName}:{method.Name}";
        }

        private const BindingFlags Binding = BindingFlags.Instance
                                           | BindingFlags.Public
                                           | BindingFlags.NonPublic;
    }
}
