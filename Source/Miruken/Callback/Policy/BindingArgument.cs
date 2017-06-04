namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class BindingArgument : ArgumentRule
    {
        public static readonly BindingArgument
            Instance = new BindingArgument();

        private BindingArgument()
        {
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute, 
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            return typeof(PolicyMethodBinding).IsAssignableFrom(paramType);
        }

        public override object Resolve(
            object callback, PolicyMethodBinding binding, IHandler composer)
        {
            return binding;
        }
    }
}
