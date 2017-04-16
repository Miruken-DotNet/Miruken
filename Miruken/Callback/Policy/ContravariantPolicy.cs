namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class ContravariantPolicy : CallbackPolicy, IComparer<Type>
    {
        public Func<MethodRule, MethodDispatch, DefinitionAttribute,
               ContravariantMethod> Binder { get; set; }

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

        public MethodBinding Bind(MethodRule rule, MethodDispatch dispatch,
                                  DefinitionAttribute attribute)
        {
            var definition = Binder?.Invoke(rule, dispatch, attribute)
                ?? new ContravariantMethod(rule, dispatch, attribute);
            InferVariance(definition);
            return definition;
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
                   (type.IsGenericType && key.IsGenericTypeDefinition &&
                    type.GetGenericTypeDefinition() == key);
        }

        int IComparer<Type>.Compare(Type x, Type y)
        {
            if (x == y) return 0;
            return x?.IsAssignableFrom(y) == true ? 1 : -1;
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
    {
        public ContravariantPolicyBuilder(ContravariantPolicy policy)
        {
            Policy = policy;
        }

        protected ContravariantPolicy Policy { get; }

        public CallbackArgument Callback => CallbackArgument.Instance;
        public ComposerArgument Composer => ComposerArgument.Instance;

        public ContravariantPolicyBuilder MatchMethod(params ArgumentRule[] args)
        {
            Policy.AddMethodRule(new MethodRule(Policy.Bind,
                ReturnsType<bool>.OrVoid, args));
            return this;
        }

        public ContravariantPolicyBuilder Binder(
            Func<MethodRule, MethodDispatch, DefinitionAttribute,
                 ContravariantMethod> creator)
        {
            Policy.Binder = creator;
            return this;
        }
    }

    public class ContravariantPolicyBuilder<Cb> : ContravariantPolicyBuilder
    {
        public ContravariantPolicyBuilder(ContravariantPolicy<Cb> policy)
            : base(policy)
        {
        }

        protected new ContravariantPolicy<Cb> Policy =>
            (ContravariantPolicy<Cb>)base.Policy;

        public new CallbackArgument<Cb> Callback => CallbackArgument<Cb>.Instance;
        public TargetArgument<Cb> Target =>  new TargetArgument<Cb>(Policy.Target);

        public ExtractArgument<Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Cb, Res>(extract);
        }

        public new ContravariantPolicyBuilder<Cb> MatchMethod(params ArgumentRule[] args)
        {
            return (ContravariantPolicyBuilder<Cb>)base.MatchMethod(args);
        }
    }
}
