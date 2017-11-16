namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public class ContravariantPolicy : CallbackPolicy
    {
        public ContravariantPolicy()
        {
            AcceptResult = VerifyResult;
        }

        public override object GetKey(object callback)
        {
            return callback?.GetType();
        }

        public override IEnumerable<object> GetCompatibleKeys(
            object key, IEnumerable output)
        {
            return CompatibleTypes(key as Type, output);
        }

        protected IEnumerable<object> CompatibleTypes(
            Type type, IEnumerable types)
        {
            if (type == null || type == typeof(object))
                return Enumerable.Empty<Tuple<object, int>>();
            return types.OfType<Type>()
                .Where(t => t != type && IsCompatible(type, t));
        }

        public override int Compare(object key1, object key2)
        {
            if (key1 == key2) return 0;
            var type1 = key1 as Type;
            if (type1 == null) return 1;
            var type2 = key2 as Type;
            if (type2 == null) return -1;
            if (type2.ContainsGenericParameters)
                return type1.ContainsGenericParameters
                     ? type2.GetGenericArguments().Length -
                       type1.GetGenericArguments().Length
                     : -1;
            if (type1.ContainsGenericParameters)
                return 1;
            return type2.IsAssignableFrom(type1) ? -1 : 1;
        }

        private static bool VerifyResult(object result, MethodBinding binding)
        {
            return result != null || binding.Dispatcher.IsVoid;
        }

        private static bool IsCompatible(Type type, Type key)
        {
            if (type == key) return true;
            if (type == null || key == null) return false;
            if (key.IsAssignableFrom(type)) return true;
            return type.GetOpenTypeConformance(key) != null;
        }

        public static ContravariantPolicy Create(
            Action<ContravariantPolicyBuilder> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new ContravariantPolicy();
            var builder = new ContravariantPolicyBuilder(policy);
            build(builder);
            return policy;
        }

        public static ContravariantPolicy<Cb> Create<Cb>(
            Func<Cb, object> target,
            Action<ContravariantPolicyBuilder<Cb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new ContravariantPolicy<Cb>(target);
            var builder = new ContravariantPolicyBuilder<Cb>(policy);
            build(builder);
            return policy;
        }
    }

    public class ContravariantPolicy<Cb> : ContravariantPolicy
    {
        public ContravariantPolicy(Func<Cb, object> target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            Target = target;
        }

        public Func<Cb, object> Target { get; }

        public override object GetKey(object callback)
        {
            return callback is Cb
                 ? GetTargetType((Cb)callback)
                 : callback?.GetType();
        }

        public override IEnumerable<object> GetCompatibleKeys(
            object key, IEnumerable output)
        {
            return CompatibleTypes(key as Type, output);
        }

        private Type GetTargetType(Cb callback)
        {
            var target = Target(callback);
            return target as Type ?? target?.GetType();
        }
    }

    public class ContravariantPolicyBuilder
        : CallbackPolicyBuilder<ContravariantPolicy, ContravariantPolicyBuilder>
    {
        public ContravariantPolicyBuilder(ContravariantPolicy policy)
            : base(policy)
        {
        }

        public CallbackArgument Callback => CallbackArgument.Instance;

        public ContravariantPolicyBuilder MatchMethodWithCallback(params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument))
                MatchMethod(args.Concat(new [] { Callback }).ToArray());
            MatchMethod(args);
            return this;
        }

        public ContravariantPolicyBuilder MatchMethodWithCallback(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument))
                MatchMethod(returnRule, args.Concat(new[] { Callback }).ToArray());
            MatchMethod(returnRule, args);
            return this;
        }
    }

    public class ContravariantPolicyBuilder<Cb>
         : CallbackPolicyBuilder<ContravariantPolicy<Cb>, ContravariantPolicyBuilder<Cb>>
    {
        public ContravariantPolicyBuilder(ContravariantPolicy<Cb> policy)
            : base(policy)
        {
        }

        public CallbackArgument<Cb> Callback => CallbackArgument<Cb>.Instance;
        public TargetArgument<Cb>   Target   => new TargetArgument<Cb>(Policy.Target);

        public ExtractArgument<Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Cb, Res>(extract);
        }

        public ContravariantPolicyBuilder<Cb> MatchCallbackMethod(params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<Cb>))
                MatchMethod(args.Concat(new[] { Callback }).ToArray());
            MatchMethod(args);
            return this;
        }

        public ContravariantPolicyBuilder<Cb> MatchCallbackMethod(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<Cb>))
                MatchMethod(returnRule, args.Concat(new[] { Callback }).ToArray());
            MatchMethod(returnRule, args);
            return this;
        }
    }
}
