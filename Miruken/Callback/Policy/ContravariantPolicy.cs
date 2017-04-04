﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ContravariantPolicy
    {
        public static ContravariantPolicy<Attrib> For<Attrib>(
            Action<ContravariantPolicyBuilder<Attrib>> configure)
            where Attrib : DefinitionAttribute
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            var policy  = new ContravariantPolicy<Attrib>();
            var builder = new ContravariantPolicyBuilder<Attrib>(policy);
            configure(builder);
            return policy;
        }

        public static Callback<Attrib> For<Attrib>()
            where Attrib : DefinitionAttribute
        {
            return new Callback<Attrib>();
        }

        public class Callback<Attrib>
            where Attrib : DefinitionAttribute
        {
            public ContravariantPolicy<Attrib, Cb> HandlesCallback<Cb>(
                Func<Cb, object> target,
                Action<ContravariantPolicyBuilder<Attrib, Cb>> configure)
            {
                if (target == null)
                    throw new ArgumentNullException(nameof(target));
                if (configure == null)
                    throw new ArgumentNullException(nameof(configure));
                var policy  = new ContravariantPolicy<Attrib, Cb>(target);
                var builder = new ContravariantPolicyBuilder<Attrib, Cb>(policy);
                configure(builder);
                return policy;
            }
        }
    }

    public class ContravariantPolicy<Attrib> : Policy<Attrib>
        where Attrib : DefinitionAttribute
    {
        public Func<MethodInfo, MethodRule<Attrib>, Attrib,
               ContravariantMethod<Attrib>> Creator { get; set; }

        protected override MethodDefinition<Attrib> Match(
            MethodInfo method, Attrib attribute,
            IEnumerable<MethodRule<Attrib>> rules)
        {
            var match = rules.FirstOrDefault(r => r.Matches(method, attribute));
            if (match == null) return null;
            var definition = Creator?.Invoke(method, match, attribute)
                ?? new ContravariantMethod<Attrib>(method, match, attribute);
            match.Configure(definition);
            return definition;
        }
    }

    public class ContravariantPolicy<Attrib, Cb> : ContravariantPolicy<Attrib>
        where Attrib : DefinitionAttribute
    {
        public ContravariantPolicy(Func<Cb, object> target)
        {
            Target = target;
        }

        public Func<Cb, object> Target { get; }
    }

    public class ContravariantPolicyBuilder<Attrib>
        where Attrib : DefinitionAttribute
    {
        public ContravariantPolicyBuilder(ContravariantPolicy<Attrib> policy)
        {
            Policy = policy;
        }

        protected ContravariantPolicy<Attrib> Policy { get; }

        public CallbackArgument<Attrib> Callback => CallbackArgument<Attrib>.Instance;
        public ComposerArgument<Attrib> Composer => ComposerArgument<Attrib>.Instance;

        public ContravariantPolicyBuilder<Attrib> MatchMethod(
            params ArgumentRule<Attrib>[] args)
        {
            Policy.AddMethod(new MethodRule<Attrib>(
                ReturnsType<bool, Attrib>.OrVoid, args));
            return this;
        }

        public ContravariantPolicyBuilder<Attrib> Create(
            Func<MethodInfo, MethodRule<Attrib>, Attrib,
                ContravariantMethod<Attrib>> creator)
        {
            Policy.Creator = creator;
            return this;
        }
    }

    public class ContravariantPolicyBuilder<Attrib, Cb> : ContravariantPolicyBuilder<Attrib>
        where Attrib : DefinitionAttribute
    {
        public ContravariantPolicyBuilder(ContravariantPolicy<Attrib, Cb> policy)
            : base(policy)
        {
        }

        protected new ContravariantPolicy<Attrib, Cb> Policy =>
            (ContravariantPolicy<Attrib, Cb>)base.Policy;

        public new CallbackArgument<Attrib, Cb> Callback =>
            CallbackArgument<Attrib, Cb>.Instance;

        public TargetArgument<Attrib, Cb> Target => 
            new TargetArgument<Attrib, Cb>(Policy.Target);

        public ExtractArgument<Attrib, Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Attrib, Cb, Res>(extract);
        }

        public new ContravariantPolicyBuilder<Attrib, Cb> MatchMethod(
            params ArgumentRule<Attrib>[] args)
        {
            return (ContravariantPolicyBuilder<Attrib, Cb>)base.MatchMethod(args);
        }
    }
}
