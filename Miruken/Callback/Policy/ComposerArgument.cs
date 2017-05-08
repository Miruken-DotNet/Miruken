namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class ComposerArgument : ArgumentRule
    {
        public static readonly ComposerArgument 
            Instance = new ComposerArgument();

        private ComposerArgument()
        {    
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            return typeof(IHandler).IsAssignableFrom(paramType);
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return composer;
        }
    }
}