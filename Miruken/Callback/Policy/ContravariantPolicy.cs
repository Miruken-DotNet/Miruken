namespace Miruken.Callback.Policy
{
    using System;

    public class ContravariantPolicy
    {
        public static ContravariantPolicy<Attrib> For<Attrib>(
            Action<ContravariantPolicyBuilder<Attrib>> configure)
            where Attrib : ContravariantAttribute
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            var policy  = new ContravariantPolicy<Attrib>();
            var builder = new ContravariantPolicyBuilder<Attrib>(policy);
            configure(builder);
            return policy;
        }

        public static Callback<Attrib> For<Attrib>()
            where Attrib : ContravariantAttribute
        {
            return new Callback<Attrib>();
        }

        public class Callback<Attrib>
            where Attrib : ContravariantAttribute
        {
            public static ContravariantPolicy<Attrib> HandlesCallback<Cb>(
                Action<ContravariantPolicyBuilder<Attrib>> configure)
            {
                if (configure == null)
                    throw new ArgumentNullException(nameof(configure));
                var policy  = new ContravariantPolicy<Attrib>();
                var builder = new ContravariantPolicyBuilder<Attrib, Cb>(policy);
                configure(builder);
                return policy;
            }
        }
    }

    public class ContravariantPolicy<Attrib> : Policy<Attrib>
        where Attrib : ContravariantAttribute
    {
    }

    public class ContravariantPolicyBuilder<Attrib>
        where Attrib : ContravariantAttribute
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
            Policy.AddMethod(new MethodRule<Attrib>(ReturnsBoolOrVoid<Attrib>.Instance, args));
            return this;
        }
    }

    public class ContravariantPolicyBuilder<Attrib, Cb> 
        : ContravariantPolicyBuilder<Attrib>
        where Attrib : ContravariantAttribute
    {
        public ContravariantPolicyBuilder(ContravariantPolicy<Attrib> policy)
            : base(policy)
        {
        }

        public new CallbackArgument<Attrib, Cb> Callback =>
            CallbackArgument<Attrib, Cb>.Instance;

        public TargetArgument<Attrib, Cb> Target(Func<Cb, object> target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return new TargetArgument<Attrib, Cb>(target);
        }

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

    public class ReturnsBoolOrVoid<Attrib> : ReturnRule<Attrib>
      where Attrib : DefinitionAttribute
    {
        public static readonly ReturnsBoolOrVoid<Attrib>
            Instance = new ReturnsBoolOrVoid<Attrib>();

        private ReturnsBoolOrVoid()
        {            
        }

        public override bool Matches(Attrib definition, Type returnType)
        {
            return returnType == typeof(void) || returnType == typeof(bool);
        }
    }
}
