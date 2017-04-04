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

        public override Type CallbackType => typeof(Cb);

        public Func<Cb, object> Key { get; }

        public Func<MethodInfo, MethodRule<Attrib>, Attrib,
               Func<object, Type>, CovariantMethod<Attrib>>
               Creator { get; set; }

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
            return definition;
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

        public CovariantPolicyBuilder<Attrib, Cb> Create(
         Func<MethodInfo, MethodRule<Attrib>, Attrib, Func<object, Type>,
             CovariantMethod<Attrib>> creator)
        {
            Policy.Creator = creator;
            return this;
        }
    }
}
