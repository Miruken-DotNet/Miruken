namespace Miruken.Callback.Policy
{
    using System.Reflection;

    public abstract class ArgumentRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public abstract bool Matches(
            MethodDefinition<Attrib> method, ParameterInfo parameter);

        public abstract object Resolve(object callback, IHandler composer);
    }
}