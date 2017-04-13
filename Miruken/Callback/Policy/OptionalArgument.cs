namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public interface IOptional {}

    public class OptionalArgument<Attrib> : ArgumentRule<Attrib>, IOptional
        where Attrib : DefinitionAttribute
    {
        private readonly ArgumentRule<Attrib> _argument;

        public OptionalArgument(ArgumentRule<Attrib> argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));
            _argument = argument;
        }

        public override bool Matches(ParameterInfo parameter, Attrib attribute)
        {
            return _argument.Matches(parameter, attribute);
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return _argument.Resolve(callback, composer);
        }
    }
}
