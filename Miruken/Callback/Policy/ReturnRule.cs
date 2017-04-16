namespace Miruken.Callback.Policy
{
    using System;

    public abstract class ReturnRule
    {
        public VoidReturn OrVoid => new VoidReturn(this);

        public abstract bool Matches(Type returnType, DefinitionAttribute attribute);

        public virtual void Configure(MethodBinding binding)
        {
        }
    }

    public class VoidReturn : ReturnRule
    {
        private readonly ReturnRule _rule;

        public VoidReturn(ReturnRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            _rule = rule;
        }

        public override bool Matches(Type returnType, DefinitionAttribute attribute)
        {
            return returnType == typeof(void) || _rule.Matches(returnType, attribute);
        }

        public override void Configure(MethodBinding binding)
        {
            if (!binding.Dispatcher.IsVoid) _rule.Configure(binding);
        }
    }
}