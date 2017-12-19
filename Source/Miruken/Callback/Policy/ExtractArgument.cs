namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Infrastructure;

    public class ExtractArgument<TCb, TRes> : ArgumentRule
    {
        private readonly Func<TCb, TRes> _extract;
        private string _alias;

        public ExtractArgument(Func<TCb, TRes> extract)
        {
            _extract = extract 
                    ?? throw new ArgumentNullException(nameof(extract));
        }

        public ExtractArgument<TCb, TRes> this[string alias]
        {
            get
            {
                if (string.IsNullOrEmpty(alias))
                    throw new ArgumentException(@"Alias cannot be empty", nameof(alias));
                _alias = alias;
                return this;
            }
        }

        public override bool Matches(
            ParameterInfo parameter, CategoryAttribute category,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            if (!paramType.Is<TRes>()) return false;
            if (_alias != null)
                aliases.Add(_alias, paramType);
            return true;
        }

        public override object Resolve(object callback)
        {
            return _extract((TCb)callback);
        }
    }
}