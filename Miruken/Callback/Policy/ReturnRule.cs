namespace Miruken.Callback.Policy
{
    using System;

    public abstract class ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public VoidReturn<Attrib> OrVoid => 
            new VoidReturn<Attrib>(this);

        public abstract bool Matches(Type returnType, Attrib attribute);

        public virtual void Configure(MethodDefinition<Attrib> method)
        {
        }
    }

    public class VoidReturn<Attrib> : ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly ReturnRule<Attrib> _rule;

        public VoidReturn(ReturnRule<Attrib> rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            _rule = rule;
        }

        public override bool Matches(Type returnType, Attrib attribute)
        {
            return returnType == typeof(void) || _rule.Matches(returnType, attribute);
        }

        public override void Configure(MethodDefinition<Attrib> method)
        {
            if (!method.IsVoid) _rule.Configure(method);
        }
    }
}