namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class ExtractArgument<Cb, Res> : ArgumentRule
    {
        private readonly Func<Cb, Res> _extract;
        private string _alias;

        public ExtractArgument(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            _extract = extract;
        }

        public ExtractArgument<Cb, Res> this[string alias]
        {
            get
            {
                if (string.IsNullOrEmpty(alias))
                    throw new ArgumentException("Alias cannot be empty", nameof(alias));
                _alias = alias;
                return this;
            }
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            if (typeof(Res).IsAssignableFrom(paramType))
            {
                if (_alias != null)
                    aliases.Add(_alias, paramType);
                return true;
            }
            return false;
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return _extract((Cb)callback);
        }
    }
}