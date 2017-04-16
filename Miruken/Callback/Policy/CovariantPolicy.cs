namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class CovariantPolicy : CallbackPolicy
    {
        public static CovariantPolicy<Cb> Create<Cb>(
            Func<Cb, object> target,
            Action<CovariantPolicyBuilder<Cb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new CovariantPolicy<Cb>(target);
            var builder = new CovariantPolicyBuilder<Cb>(policy);
            build(builder);
            return policy;
        }
    }

    public class CovariantPolicy<Cb> : CovariantPolicy, IComparer<Type>
    {
        public CovariantPolicy(Func<Cb, object> key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Key = key;
        }

        public Func<Cb, object> Key { get; }

        public Func<MethodRule, MethodDispatch, DefinitionAttribute,
               Func<object, Type>, CovariantMethod> Binder { get; set; }

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

        public MethodBinding Bind(MethodRule rule, MethodDispatch dispatch,
                                  DefinitionAttribute attribute)
        {
            Func<object, Type> returnType = cb => Key((Cb)cb) as Type;
            var definition = Binder?.Invoke(rule, dispatch, attribute, returnType)
                ?? new CovariantMethod(rule, dispatch, attribute, returnType);
            InferVariance(definition);
            return definition;
        }

        private static void InferVariance(MethodBinding method)
        {
            var key      = method.Attribute.Key;
            var restrict = key as Type;
            if (restrict != null)
            {
                if (method.VarianceType == null ||
                    method.VarianceType.IsAssignableFrom(restrict))
                    method.VarianceType = restrict;
            }
        }

        private static bool AcceptKey(Type type, Type key)
        {
            return key.IsGenericTypeDefinition
                 ? type.IsGenericType && type.GetGenericTypeDefinition() == key
                 : type.IsAssignableFrom(key);
        }

        int IComparer<Type>.Compare(Type x, Type y)
        {
            if (x == y) return 0;
            return x?.IsAssignableFrom(y) == true ? -1 : 1;
        }
    }

    public class CovariantPolicyBuilder<Cb>
    {
        public CovariantPolicyBuilder(CovariantPolicy<Cb> policy)
        {
            Policy = policy;
        }

        protected CovariantPolicy<Cb> Policy { get; }

        public CallbackArgument<Cb> Callback => CallbackArgument<Cb>.Instance;
        public ComposerArgument     Composer => ComposerArgument.Instance;
        public ReturnsKey           Return   => ReturnsKey.Instance;

        public ExtractArgument<Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Cb, Res>(extract);
        }

        public CovariantPolicyBuilder<Cb> MatchMethod(params ArgumentRule[] args)
        {
            Policy.AddMethodRule(new MethodRule(Policy.Bind, args));
            return this;
        }

        public CovariantPolicyBuilder<Cb> MatchMethod(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            Policy.AddMethodRule(new MethodRule(Policy.Bind, returnRule, args));
            return this;
        }

        public CovariantPolicyBuilder<Cb> BindMethod(
         Func<MethodRule, MethodDispatch, DefinitionAttribute, Func<object, Type>,
             CovariantMethod> binder)
        {
            Policy.Binder = binder;
            return this;
        }
    }
}
