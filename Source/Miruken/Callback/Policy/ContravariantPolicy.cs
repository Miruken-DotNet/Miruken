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

        public override IEnumerable<Tuple<object, int>> GetCompatibleKeys(
            object key, IEnumerable output)
        {
            return CompatibleTypes(key as Type, output);
        }

        protected IEnumerable<Tuple<object, int>> CompatibleTypes(
            Type type, IEnumerable types)
        {
            if (type == null || type == typeof(object))
                return Enumerable.Empty<Tuple<object, int>>();
            return types.OfType<Type>()
                .Where(t => t != type)
                .Select(t => new { Key = t, Accuracy = Accuracy(type, t) })
                .Where(k => k.Accuracy.HasValue)
                .OrderBy(k => k.Accuracy)
                .Select(k => Tuple.Create((object)k.Key, k.Accuracy.Value));
        }

        private static bool VerifyResult(object result, MethodBinding binding)
        {
            return result != null || binding.Dispatcher.IsVoid;
        }

        private static int? Accuracy(Type type, Type key)
        {
            if (type == key) return 0;
            if (type == null || key == null) return null;
            if (key.IsAssignableFrom(type))
                return GetClassAccuracy(type, key);
            var open = type.GetOpenTypeConformance(key);
            return open != null 
                 ? 1000 - open.GenericTypeArguments.Length
                 : (int?)null;
        }

        private static int GetClassAccuracy(Type type, Type key)
        {
            if (type.IsClass && key.IsClass)
            {
                var accuracy = 0;
                while (type != null && type != key)
                {
                    type = type.BaseType;
                    ++accuracy;
                }
                return accuracy;
            }
            return 50;
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

        public override IEnumerable<Tuple<object, int>> GetCompatibleKeys(
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
