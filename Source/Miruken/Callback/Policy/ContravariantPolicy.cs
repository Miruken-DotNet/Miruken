namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public class ContravariantPolicy : CallbackPolicy, IComparer<Type>
    {
        public ContravariantPolicy()
        {
            AcceptResult = VerifyResult;
        }

        public override object GetKey(object callback)
        {
            return callback?.GetType();
        }

        public override IEnumerable SelectKeys(object callback, Keys keys)
        {
            return SelectTypeKeys(callback?.GetType(), keys);
        }

        protected IEnumerable SelectTypeKeys(Type type, Keys keys)
        {
            if (type == null || type == typeof(object))
                return Enumerable.Empty<object>();
            return keys.Typed.Where(k => AcceptKey(type, k))
                       .OrderBy(t => t, this);
        }

        private static bool AcceptKey(Type type, Type key)
        {
            return type.IsClassOf(key);
        }

        private static bool VerifyResult(object result, MethodBinding binding)
        {
            return result != null || binding.Dispatcher.IsVoid;
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

        public override object GetKey(object callback)
        {
            return callback is Cb
                 ? Target((Cb)callback)?.GetType()
                 : callback?.GetType();
        }

        public override IEnumerable SelectKeys(object callback, Keys keys)
        {
            if (!(callback is Cb))
                return SelectTypeKeys(callback?.GetType(), keys);
            var type = Target((Cb)callback)?.GetType();
            return SelectTypeKeys(type, keys);
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
