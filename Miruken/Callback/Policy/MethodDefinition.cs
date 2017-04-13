namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    #region Method Binding

    public enum MethodBinding
    {
        LateBound,
        OpenGeneric,
        FastNoArgsVoid,
        FastOneArgVoid,
        FastTwoArgsVoid,
        FastThreeArgsVoid,
        FastNoArgsReturn,
        FastOneArgReturn,
        FastTwoArgsReturn,
        FastThreeArgsReturn
    }

    #endregion

    public abstract class MethodDefinition
    {
        private Type _varianceType;
        private Delegate _delegate;
        private MethodBinding _binding;
        private Tuple<int, int>[] _mapping;
        private List<ICallbackFilter> _filters;

        protected MethodDefinition(MethodInfo method)
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

        public Type VarianceType
        {
            get { return _varianceType; }
            set
            {
                if (value?.ContainsGenericParameters == true &&
                    !value.IsGenericTypeDefinition)
                    _varianceType = value.GetGenericTypeDefinition();
                else
                    _varianceType = value;
            }
        }

        public abstract object GetKey();

        public bool Accepts(object callback, IHandler composer)
        {
            return _filters?.All(f => f.Accepts(callback, composer)) != false;
        }

        public abstract bool Dispatch(object target, object callback, IHandler composer);

        internal void AddFilters(params ICallbackFilter[] filters)
        {
            if (filters == null || filters.Length == 0) return;
            if (_filters == null) _filters = new List<ICallbackFilter>();
            _filters.AddRange(filters);
        }

        protected object Invoke(object target, object[] args, Type returnType = null)
        {
            switch (_binding)
            {
                #region Fast Invocation
                case MethodBinding.FastNoArgsVoid:
                    AssertArgsCount(0, args);
                    ((NoArgsDelegate)_delegate)(target);
                    return null;
                case MethodBinding.FastOneArgVoid:
                    AssertArgsCount(1, args);
                    ((OneArgDelegate)_delegate)(target, args[0]);
                    return null;
                case MethodBinding.FastTwoArgsVoid:
                    AssertArgsCount(2, args);
                    ((TwoArgsDelegate)_delegate)(target, args[0], args[1]);
                    return null;
                case MethodBinding.FastThreeArgsVoid:
                    AssertArgsCount(3, args);
                    ((ThreeArgsDelegate)_delegate)(target, args[0], args[1], args[2]);
                    return null;
                case MethodBinding.FastNoArgsReturn:
                    AssertArgsCount(0, args);
                    return ((NoArgsReturnDelegate)_delegate)(target);
                case MethodBinding.FastOneArgReturn:
                    AssertArgsCount(1, args);
                    return ((OneArgReturnDelegate)_delegate)(target, args[0]);
                case MethodBinding.FastTwoArgsReturn:
                    AssertArgsCount(2, args);
                    return ((TwoArgsReturnDelegate)_delegate)(target, args[0], args[1]);
                case MethodBinding.FastThreeArgsReturn:
                    AssertArgsCount(3, args);
                    return ((ThreeArgsReturnDelegate)_delegate)(target, args[0], args[1], args[2]);
                #endregion
                default:
                    return InvokeLate(target, args, returnType);
            }
        }

        protected object InvokeLate(object target, object[] args, Type returnType = null)
        {
            var method     = Method;
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
                            _binding  = MethodBinding.FastNoArgsVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncNoArgs(method);
                            _binding  = MethodBinding.FastNoArgsReturn;
                        }
                        return;
                    case 1:
                        if (isVoid)
                        {
                            _delegate = RuntimeHelper.CreateActionOneArg(method);
                            _binding  = MethodBinding.FastOneArgVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncOneArg(method);
                            _binding  = MethodBinding.FastOneArgReturn;
                        }
                        return;
                    case 2:
                        if (isVoid)
                        {
                            _delegate = RuntimeHelper.CreateActionTwoArgs(method);
                            _binding  = MethodBinding.FastTwoArgsVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncTwoArgs(method);
                            _binding  = MethodBinding.FastTwoArgsReturn;
                        }
                        return;
                    case 3:
                        if (isVoid)
                        {
                            _delegate = RuntimeHelper.CreateActionThreeArgs(method);
                            _binding  = MethodBinding.FastThreeArgsVoid;
                        }
                        else
                        {
                            _delegate = RuntimeHelper.CreateFuncThreeArgs(method);
                            _binding  = MethodBinding.FastThreeArgsReturn;
                        }
                        return;
                    #endregion
                    default:
                        _binding = MethodBinding.LateBound;
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
            var methodArgs = method.GetGenericArguments();
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

            _mapping = typeMapping;
            _binding = MethodBinding.OpenGeneric;
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

    public abstract class MethodDefinition<Attrib> : MethodDefinition
        where Attrib : DefinitionAttribute
    {
        protected MethodDefinition(MethodInfo method, 
                                   MethodRule<Attrib> rule, 
                                   Attrib attribute)
            : base(method)
        {
            Rule       = rule;
            Attribute  = attribute;
            var filter = attribute as ICallbackFilter;
            if (filter != null) AddFilters(filter);
        }

        public MethodRule<Attrib> Rule      { get; }
        public Attrib             Attribute { get; }

        public override object GetKey()
        {
            var key = Attribute.Key;
            return key == null || key is Type ? VarianceType : key;
        }

        public override bool Dispatch(object target, object callback, IHandler composer)
        {
            return Accepts(callback, composer) && VerifyResult(target, callback, composer);
        }

        protected virtual bool VerifyResult(object target, object callback, IHandler composer)
        {
            Invoke(target, callback, composer);
            return true;
        }

        protected object Invoke(object target, object callback,
                                IHandler composer, Type returnType = null)
        {
            var args = Rule.ResolveArgs(this, callback, composer);
            return Invoke(target, args, returnType);
        }
    }
}
