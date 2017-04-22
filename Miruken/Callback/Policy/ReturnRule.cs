namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public abstract class ReturnRule
    {
        public VoidReturn OrVoid => new VoidReturn(this);

        public abstract bool Matches(
            Type returnType, ParameterInfo[] parameters,
            DefinitionAttribute attribute);

        public virtual void Configure(MethodBinding binding) { }

    }

    public abstract class ReturnRuleDecorator : ReturnRule
    {
        protected ReturnRuleDecorator(ReturnRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            Rule = rule;
        }

        public ReturnRule Rule { get; }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            DefinitionAttribute attribute)
        {
            return Rule.Matches(returnType, parameters, attribute);
        }

        public override void Configure(MethodBinding binding)
        {
            Rule.Configure(binding);
        }
    }

    public class VoidReturn : ReturnRuleDecorator
    {
        public VoidReturn(ReturnRule rule) : base(rule)
        {
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            DefinitionAttribute attribute)
        {
            return returnType == typeof(void) ||
                Rule.Matches(returnType, parameters, attribute);
        }

        public override void Configure(MethodBinding binding)
        {
            if (!binding.Dispatcher.IsVoid) Rule.Configure(binding);
        }
    }
}