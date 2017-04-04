namespace Miruken.Callback.Policy
{
    using System.Reflection;

    public abstract class ArgumentRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public abstract bool Matches(
           ParameterInfo parameter, Attrib attribute);

        public virtual void Configure(
            ParameterInfo parameter, MethodDefinition<Attrib> method)
        {         
        }

        public abstract object Resolve(object callback, IHandler composer);
    }
}