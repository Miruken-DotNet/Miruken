namespace Miruken.Callback.Policy
{
    using System.Reflection;

    public class ComposerArgument<Attrib> : ArgumentRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public static readonly ComposerArgument<Attrib> 
            Instance = new ComposerArgument<Attrib>();

        private ComposerArgument()
        {    
        }

        public override bool Matches(Attrib definition, ParameterInfo parameter)
        {
            var paramType = parameter.ParameterType;
            return typeof(IHandler).IsAssignableFrom(paramType);
        }

        public override object Resolve(Attrib definition, object callback, IHandler composer)
        {
            return composer;
        }
    }
}