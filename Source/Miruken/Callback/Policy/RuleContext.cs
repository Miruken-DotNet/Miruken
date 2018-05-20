namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;

    public class RuleContext
    {
        private Dictionary<string, Type> _aliases;
        private List<string> _errors;

        public RuleContext(CategoryAttribute category)
        {
            Category = category;
        }

        public CategoryAttribute Category { get; }

        public bool HasErrors => _errors?.Count > 0;

        public string[] Errors => _errors?.ToArray() ?? Array.Empty<string>();

        public bool AddAlias(string alias, Type type)
        {
            if (_aliases == null)
            {
                _aliases = new Dictionary<string, Type>
                {
                    { alias, type }
                };
                return true;
            }
            Type aliasedType;
            if (_aliases.TryGetValue(alias, out aliasedType))
            {
                if (aliasedType != type)
                {
                    AddError("Mismatched alias '{alias}', {type} != {aliasType}");
                    return false;
                }
                return true;
            }
           _aliases.Add(alias, type);
            return true;
        }

        public void AddError(string error)
        {
            if (_errors == null)
                _errors = new List<string>();
            _errors.Add(error);
        }
    }
}
