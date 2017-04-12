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
            if (_extract == null)
                throw new ArgumentNullException(nameof(extract));
            _extract = extract;
        }

        public override bool Matches(ParameterInfo parameter, Attrib attribute)
        {
            var paramType = parameter.ParameterType;
            return typeof(Res).IsAssignableFrom(paramType);
        }

        public override void Configure(
            ParameterInfo parameter, MethodDefinition<Attrib> method)
        {
            method.AddFilters(GetFilters(parameter, cb => _extract((Cb)cb)));
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return _extract((Cb)callback);
        }
    }
}