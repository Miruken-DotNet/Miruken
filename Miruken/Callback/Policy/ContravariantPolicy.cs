namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    public class ContravariantPolicy : CallbackPolicy, IComparer<Type>
    {
        public ContravariantPolicy()
        {
            NoResult  = false;
            HasResult = IsResult;
        }

        public override MethodBinding BindMethod(
            MethodRule rule, MethodDispatch dispatch,
            DefinitionAttribute attribute)
        {
            var binding = base.BindMethod(rule, dispatch, attribute);
            InferVariance(binding);
            return binding;
        }

        public override IEnumerable SelectKeys(object callback, ICollection keys)
        {
            return SelectKeys(callback?.GetType(), keys);
        }

        protected IEnumerable SelectKeys(Type type, ICollection keys)
        {
            if (type == null || type == typeof(object))
                return Enumerable.Empty<object>();
            return keys.OfType<Type>().Where(k => AcceptKey(type, k))
                       .OrderBy(t => t, this);
        }

        private static void InferVariance(MethodBinding method)
        {
            var restrict = method.Attribute.Key as Type;
            if (restrict != null)
            {
                if (method.VarianceType == null)
                    method.VarianceType = restrict;
            }
        }

        private static bool AcceptKey(Type type, Type key)
        {
            return key.IsAssignableFrom(type) ||
                   type.GetOpenImplementation(key) != null;
        }

        private static bool IsResult(object result)
        {
            return result == null || true.Equals(result);
        }

        int IComparer<Type>.Compare(Type x, Type y)
        {
            if (x == y) return 0;
            return x == null || AcceptKey(x, y) ? -1 : 1;
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

        public override IEnumerable SelectKeys(object callback, ICollection keys)
        {
            if (!(callback is Cb))
                return Enumerable.Empty<object>();
            var type = Target((Cb)callback)?.GetType();
            return SelectKeys(type, keys);
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
    }
}
