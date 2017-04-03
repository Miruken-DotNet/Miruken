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

    public class CovariantPolicy<Attrib, Cb> : Policy<Attrib>
        where Attrib : DefinitionAttribute
    {
        public CovariantPolicy(Func<Cb, object> key)
        {
            Key = key;
        }

        public Func<Cb, object> Key { get; }

        protected override MethodDefinition<Attrib> Match(
             MethodInfo method, Attrib attribute,
             IEnumerable<MethodRule<Attrib>> rules)
        {
            return rules.Select(rule => {
                var candidate = new CovariantMethod<Attrib>(
                    method, rule, attribute, cb => Key((Cb)cb) as Type);
                return rule.Matches(candidate) ? candidate : null;
                })
                .FirstOrDefault(definition => definition != null);
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
            Policy.AddMethod(new MethodRule<Attrib>(args));
            return this;
        }

        public CovariantPolicyBuilder<Attrib, Cb> MatchMethod(
            ReturnRule<Attrib> returnRule, params ArgumentRule<Attrib>[] args)
        {
            Policy.AddMethod(new MethodRule<Attrib>(returnRule, args));
            return this;
        }
    }
}
