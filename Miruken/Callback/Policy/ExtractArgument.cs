namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public class ExtractArgument<Attrib, Cb, Res> : ArgumentRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly Func<Cb, Res> _extract;

        public ExtractArgument(Func<Cb, Res> extract)
        {
            _extract = extract;
        }

        public override bool Matches(
            MethodDefinition<Attrib> method, ParameterInfo parameter)
        {
            var paramType = parameter.ParameterType;
            return typeof(Res).IsAssignableFrom(paramType);
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return _extract((Cb)callback);
        }
    }
}