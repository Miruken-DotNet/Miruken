namespace Miruken.Callback.Policy
{
    using System;

    public abstract class ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public OptionalReturn<Attrib> Optional => 
            new OptionalReturn<Attrib>(this);

        public abstract bool Matches(MethodDefinition<Attrib> method);
    }

    public class OptionalReturn<Attrib> : ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly ReturnRule<Attrib> _rule;

        public OptionalReturn(ReturnRule<Attrib> rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            _rule = rule;
        }

        public override bool Matches(MethodDefinition<Attrib> method)
        {
            return method.IsVoid || _rule.Matches(method);
        }
    }
}