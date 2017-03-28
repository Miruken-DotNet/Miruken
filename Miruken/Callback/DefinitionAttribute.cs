namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;
    using Policy;

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

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public abstract class DefinitionAttribute 
        : Attribute, IComparable<DefinitionAttribute>
    {
        private Delegate _methodDelegate;
        private Tuple<int, int>[] _typeMapping;
        private readonly List<ICallbackFilter> _filters;

        public    object        Key           { get; set; }
        public    bool          Invariant     { get; set; }
        public    Type          VarianceType  { get; internal set; }
        protected MethodInfo    Method        { get; private set; }
        protected MethodBinding MethodBinding { get; private set; }

        public bool Untyped => VarianceType == null || VarianceType == typeof(object);

        protected DefinitionAttribute()
        {
            _filters = new List<ICallbackFilter>();
        }

        public void Init(MethodInfo method)
        {
            if (Method != null)
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName} already initialized with {Method.ReflectedType?.FullName}:{Method.Name}");
            }
            Match(method);
            Configrue(method);
        }

        protected abstract void Match(MethodInfo method);

        protected virtual bool Accepts(object callback, IHandler composer)
        {
            return _filters.All(f => f.Accepts(this, callback, composer));
        }

        public virtual bool Dispatch(object target, object callback, IHandler composer)
        {
            if (!Accepts(callback, composer)) return false;
            var args = ResolveArgs(callback, composer);
            return Dispatched(Invoke(target, args));
        }

        protected abstract object[] ResolveArgs(object callback, IHandler composer);

        public abstract int CompareTo(DefinitionAttribute other);

        internal void AddFilters(params ICallbackFilter[] filters)
        {
            _filters.AddRange(filters);    
        }

        protected virtual object Invoke(object target, object[] args)
        {
            switch (MethodBinding)
            {
                case MethodBinding.FastNoArgsVoid:
                    AssertArgsCount(0, args);
                    ((NoArgsDelegate)_methodDelegate)(target);
                    return null;
                case MethodBinding.FastOneArgVoid:
                    AssertArgsCount(1, args);
                    ((OneArgDelegate)_methodDelegate)(target, args[0]);
                    return null;
                case MethodBinding.FastTwoArgsVoid:
                    AssertArgsCount(2, args);
                    ((TwoArgsDelegate)_methodDelegate)(target, args[0], args[1]);
                    return null;
                case MethodBinding.FastThreeArgsVoid:
                    AssertArgsCount(3, args);
                    ((ThreeArgsDelegate)_methodDelegate)(target, args[0], args[1], args[2]);
                    return null;
                case MethodBinding.FastNoArgsReturn:
                    AssertArgsCount(0, args);
                    return ((NoArgsReturnDelegate)_methodDelegate)(target);
                case MethodBinding.FastOneArgReturn:
                    AssertArgsCount(1, args);
                    return ((OneArgReturnDelegate)_methodDelegate)(target, args[0]);
                case MethodBinding.FastTwoArgsReturn:
                    AssertArgsCount(2, args);
                    return ((TwoArgsReturnDelegate)_methodDelegate)(target, args[0], args[1]);
                case MethodBinding.FastThreeArgsReturn:
                    AssertArgsCount(3, args);
                    return ((ThreeArgsReturnDelegate)_methodDelegate)(target, args[0], args[1], args[2]);
                default:
                    return InvokeLate(target, args);
            }
        }

        protected virtual bool Dispatched(object result)
        {
            return true;
        }

        protected object InvokeLate(object target, object[] args, Type returnType = null)
        {
            var method     = Method;
            var parameters = method.GetParameters();
            if (parameters.Length > (args?.Length ?? 0))
                throw new ArgumentException($"Method {method.ReflectedType?.FullName}:{method.Name} requires {parameters.Length} arguments");
            if (method.ContainsGenericParameters)
            {                
                var argTypes = _typeMapping.Select(mapping =>
                {
                    if (mapping.Item1 < 0)  // return type
                    {
                        if (returnType == null)
                            throw new ArgumentException(
                                "Return type is unknown and cannot help infer types");
                        return returnType.GetGenericArguments()[mapping.Item2];
                    }
                    var arg = args?[mapping.Item1];
                    if (arg == null)
                        throw new ArgumentException($"Argument {mapping.Item1} is null and cannot help infer types");
                    return arg.GetType().GetGenericArguments()[mapping.Item2];
                }).ToArray();
                method = method.MakeGenericMethod(argTypes);
            }
            return method.Invoke(target, HandlerDescriptor.Binding, null, args,
                                 CultureInfo.InvariantCulture);
        }

        private void Configrue(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var isVoid     = method.ReturnType == typeof(void);
            if (!method.IsGenericMethodDefinition)
            {
                switch (parameters.Length)
                {
                    case 0:
                        if (isVoid)
                        {
                            _methodDelegate = RuntimeHelper.CreateActionNoArgs(method);
                            MethodBinding   = MethodBinding.FastNoArgsVoid;
                        }
                        else
                        {
                            _methodDelegate = RuntimeHelper.CreateFuncNoArgs(method);
                            MethodBinding   = MethodBinding.FastNoArgsReturn;
                        }
                        return;
                    case 1:
                        if (isVoid)
                        {
                            _methodDelegate = RuntimeHelper.CreateActionOneArg(method);
                            MethodBinding   = MethodBinding.FastOneArgVoid;
                        }
                        else
                        {
                            _methodDelegate = RuntimeHelper.CreateFuncOneArg(method);
                            MethodBinding   = MethodBinding.FastOneArgReturn;
                        }
                        return;
                    case 2:
                        if (isVoid)
                        {
                            _methodDelegate = RuntimeHelper.CreateActionTwoArgs(method);
                            MethodBinding   = MethodBinding.FastTwoArgsVoid;
                        }
                        else
                        {
                            _methodDelegate = RuntimeHelper.CreateFuncTwoArgs(method);
                            MethodBinding   = MethodBinding.FastTwoArgsReturn;
                        }
                        return;
                    case 3:
                        if (isVoid)
                        {
                            _methodDelegate = RuntimeHelper.CreateActionThreeArgs(method);
                            MethodBinding   = MethodBinding.FastThreeArgsVoid;
                        }
                        else
                        {
                            _methodDelegate = RuntimeHelper.CreateFuncThreeArgs(method);
                            MethodBinding   = MethodBinding.FastThreeArgsReturn;
                        }

                        return;
                    default:
                        MethodBinding = MethodBinding.LateBound;
                        return;
                }
            }

            var argSources  = parameters
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
                    $"Type mapping for {method.ReflectedType?.FullName}:{method.Name} could not be inferred");

            _typeMapping  = typeMapping;
            MethodBinding = MethodBinding.OpenGeneric;
            Method        = method;
        }

        private static void AssertArgsCount(int expected, params object[] args)
        {
            if (args.Length != expected)
                throw new ArgumentException(
                    $"Expected {expected} arguments, but {args.Length} provided");
        }



        private Type _genericCallbackTypeDef;

        public bool SatisfiesGenericDefinition(Type callbackType)
        {
            return _genericCallbackTypeDef != null && callbackType.IsGenericType &&
                   _genericCallbackTypeDef == callbackType.GetGenericTypeDefinition();
        }

        protected bool ConfigureCallbackType(MethodInfo method, Type callbackType)
        {
            Configrue(method);
            if (callbackType.IsGenericType && callbackType.ContainsGenericParameters)
                _genericCallbackTypeDef = callbackType.GetGenericTypeDefinition();
            VarianceType = callbackType;
            return true;
        }
    }

    public abstract class ContravariantAttribute : DefinitionAttribute
    {
        public override int CompareTo(DefinitionAttribute other)
        {
            var otherHandler = other as ContravariantAttribute;
            if (otherHandler == null) return -1;
            if (otherHandler.VarianceType == VarianceType)
                return 0;
            if (VarianceType.IsAssignableFrom(otherHandler.VarianceType))
                return 1;
            return -1;
        }
    }

    public abstract class CovariantAttribute : DefinitionAttribute
    {
        protected bool IsCovariantType(Type type)
        {
            if (type == null) return false;
            var callbackType = VarianceType;
            return callbackType == null || callbackType == typeof(object) ||
                   (callbackType.IsGenericType && callbackType.ContainsGenericParameters
                       ? SatisfiesGenericDefinition(type)
                       : type.IsAssignableFrom(callbackType));
        }

        protected bool SatisfiesCovariant(object instance)
        {
            var type = instance.GetType();
            var callbackType = VarianceType;
            if (callbackType == null) return true;
            return Invariant ? callbackType == type
                 : callbackType.IsInstanceOfType(instance)
                     || SatisfiesGenericDefinition(type);
        }

        public override int CompareTo(DefinitionAttribute other)
        {
            var otherProvider = other as CovariantAttribute;
            if (otherProvider == null) return -1;
            if (otherProvider.VarianceType == VarianceType)
                return 0;
            if (VarianceType == null ||
                !VarianceType.IsAssignableFrom(otherProvider.VarianceType))
                return 1;
            return -1;
        }
    }
}