namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public interface IOptional {}

    public class OptionalArgument : ArgumentRule, IOptional
    {
        private readonly ArgumentRule _argument;

        public OptionalArgument(ArgumentRule argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));
            _argument = argument;
        }

        public override bool Matches(ParameterInfo parameter, DefinitionAttribute attribute)
        {
            return _argument.Matches(parameter, attribute);
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return _argument.Resolve(callback, composer);
        }
    }
}
