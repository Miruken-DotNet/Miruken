namespace Miruken.Callback.Policy
{
    using System;
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

    public class CovariantPolicy<Attrib, Cb> : CallbackPolicy<Attrib>
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

        public override bool Accepts(object callback)
        {
            return callback is Cb;
        }

        public override Type GetVarianceType(object callback)
        {
            return Accepts(callback) ? Key((Cb)callback) as Type : null;
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

        private void AssignVariance(MethodDefinition<Attrib> method)
        {
            var key      = method.Attribute.Key;
            var restrict = key as Type;
            if (restrict != null)
            {
                if (method.VarianceType == null ||
                    method.VarianceType.IsAssignableFrom(restrict))
                    method.VarianceType = restrict;
                if (restrict != typeof(object))
                    method.AddFilters(new CovariantFilter<Cb>(restrict, Key));
            }
            else if (key != null)
                method.AddFilters(new KeyEqualityFilter<Cb>(key, Key));
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
        public ReturnsKey<Attrib, Cb>       Return   => new ReturnsKey<Attrib, Cb>(Policy.Key);

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

        public CovariantPolicyBuilder<Attrib, Cb> Create(
         Func<MethodInfo, MethodRule<Attrib>, Attrib, Func<object, Type>,
             CovariantMethod<Attrib>> creator)
        {
            Policy.Creator = creator;
            return this;
        }
    }
}
