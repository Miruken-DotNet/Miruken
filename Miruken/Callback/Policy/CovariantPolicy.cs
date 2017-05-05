namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public abstract class CovariantPolicy : CallbackPolicy, IComparer<Type>
    {
        public override PolicyMethodBinding BindMethod(
            MethodRule rule, MethodDispatch dispatch,
            DefinitionAttribute attribute)
        {
            var binding = base.BindMethod(rule, dispatch, attribute);
            InferVariance(binding);
            return binding;
        }

        protected static void InferVariance(PolicyMethodBinding method)
        {
            var key = method.Attribute.Key;
            var restrict = key as Type;
            if (restrict != null)
            {
                if (method.VarianceType == null ||
                    method.VarianceType.IsAssignableFrom(restrict))
                    method.VarianceType = restrict;
            }
        }

        protected static bool AcceptKey(Type type, Type key)
        {
            return key.IsGenericTypeDefinition
                 ? type.GetOpenTypeConformance(key) != null
                 : type.IsAssignableFrom(key);
        }

        int IComparer<Type>.Compare(Type x, Type y)
        {
            if (x == y) return 0;
            return y == null || AcceptKey(x, y) ? -1 : 1;
        }

        public static CovariantPolicy<Cb> Create<Cb>(
            Func<Cb, object> target,
            Action<CovariantPolicyBuilder<Cb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy = new CovariantPolicy<Cb>(target);
            var builder = new CovariantPolicyBuilder<Cb>(policy);
            build(builder);
            return policy;
        }
    }

    public class CovariantPolicy<Cb> : CovariantPolicy
    {
        public CovariantPolicy(Func<Cb, object> key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Key        = key;
            ResultType = GetResultType;
        }

        public Func<Cb, object> Key { get; }

        public override IEnumerable SelectKeys(object callback, ICollection keys)
        {
            if (!(callback is Cb))
                return Enumerable.Empty<object>();
            var key  = Key((Cb)callback);
            var type = key as Type;
            if (type == null)
                return Enumerable.Repeat(key, 1);
            var typeKeys = keys.OfType<Type>();
            return type == typeof(object) 
                 ? typeKeys.Where(k => !k.IsGenericTypeDefinition)
                 : typeKeys.Where(k => AcceptKey(type, k))
                      .OrderBy(t => t, this);
        }

        private Type GetResultType(object callback)
        {
            return Key((Cb)callback) as Type;
        }
    }

    public class CovariantPolicyBuilder<Cb>
        : CallbackPolicyBuilder<CovariantPolicy<Cb>, CovariantPolicyBuilder<Cb>>
    {
        public CovariantPolicyBuilder(CovariantPolicy<Cb> policy)
            : base(policy)
        {
        }

        public CallbackArgument<Cb> Callback  => CallbackArgument<Cb>.Instance;
        public ReturnsKey           ReturnKey => ReturnsKey.Instance;

        public ExtractArgument<Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Cb, Res>(extract);
        }
    }
}
