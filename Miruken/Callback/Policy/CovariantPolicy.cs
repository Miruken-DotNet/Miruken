namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class CovariantPolicy
    {
        public static Key<Attrib> For<Attrib>()
            where Attrib : DefinitionAttribute
        {
            return new Key<Attrib>();
        }

        public class Key<Attrib>
            where Attrib : DefinitionAttribute
        {
            public CovariantPolicy<Attrib, Cb> HandlesCallback<Cb>(
                Func<Cb, object> key,
                Action<CovariantPolicyBuilder<Attrib, Cb>> configure)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                if (configure == null)
                    throw new ArgumentNullException(nameof(configure));
                var policy  = new CovariantPolicy<Attrib, Cb>(key);
                var builder = new CovariantPolicyBuilder<Attrib, Cb>(policy);
                configure(builder);
                return policy;
            }
        }
    }

    public class CovariantPolicy<Attrib, Cb> 
        : CallbackPolicy<Attrib>, IComparer<Type>
        where Attrib : DefinitionAttribute
    {
        public CovariantPolicy(Func<Cb, object> key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Key = key;
        }

        public Func<Cb, object> Key { get; }

        public Func<MethodInfo, MethodRule<Attrib>, Attrib,
               Func<object, Type>, CovariantMethod<Attrib>>
               Creator { get; set; }

        public override bool Accepts(object callback, IHandler composer)
        {
            return callback is Cb;
        }

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

        protected override MethodDefinition<Attrib> Match(
            MethodInfo method, Attrib attribute,
            IEnumerable<MethodRule<Attrib>> rules)
        {
            var match = rules.FirstOrDefault(r => r.Matches(method, attribute));
            if (match == null) return null;
            Func<object, Type> returnType = cb => Key((Cb)cb) as Type;
            var definition = Creator?.Invoke(method, match, attribute, returnType)
                ?? new CovariantMethod<Attrib>(method, match, attribute, returnType);
            match.Configure(definition);
            AssignVariance(definition);
            return definition;
        }

        private static void AssignVariance(MethodDefinition<Attrib> method)
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

    public class CovariantPolicyBuilder<Attrib, Cb>
        where Attrib : DefinitionAttribute
    {
        public CovariantPolicyBuilder(CovariantPolicy<Attrib, Cb> policy)
        {
            Policy = policy;
        }

        protected CovariantPolicy<Attrib, Cb>  Policy { get; }

        public CallbackArgument<Attrib, Cb> Callback => CallbackArgument<Attrib, Cb>.Instance;
        public ComposerArgument<Attrib>     Composer => ComposerArgument<Attrib>.Instance;
        public ReturnsKey<Attrib>           Return   => new ReturnsKey<Attrib>();

        public ExtractArgument<Attrib, Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Attrib, Cb, Res>(extract);
        }

        public CovariantPolicyBuilder<Attrib, Cb> MatchMethod(
            params ArgumentRule<Attrib>[] args)
        {
            Policy.AddMethodRule(new MethodRule<Attrib>(args));
            return this;
        }

        public CovariantPolicyBuilder<Attrib, Cb> MatchMethod(
            ReturnRule<Attrib> returnRule, params ArgumentRule<Attrib>[] args)
        {
            Policy.AddMethodRule(new MethodRule<Attrib>(returnRule, args));
            return this;
        }

        public CovariantPolicyBuilder<Attrib, Cb> CreateUsing(
         Func<MethodInfo, MethodRule<Attrib>, Attrib, Func<object, Type>,
             CovariantMethod<Attrib>> creator)
        {
            Policy.Creator = creator;
            return this;
        }
    }
}
