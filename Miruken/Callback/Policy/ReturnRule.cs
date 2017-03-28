namespace Miruken.Callback.Policy
{
    using System;

    public abstract class ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public abstract bool Matches(Attrib definition, Type returnType);
    }
}