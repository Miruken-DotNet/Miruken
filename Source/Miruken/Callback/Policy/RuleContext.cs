namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;

    public class RuleContext
    {
        private Dictionary<string, Type> _aliases;
        private List<string> _errors;

        public string[] Errors => _errors.ToArray();

        public bool AddAlias(string alias, Type type)
        {
            if (_aliases == null)
            {
                _aliases = new Dictionary<string, Type>
                {
                    { alias, type }
                };
            }
            else
            {
                Type aliasType;
                if (_aliases.TryGetValue(alias, out aliasType))
                {
                    if (aliasType != type)
                    {
                        AddError("Mismatched alias '{alias}': {type} != {aliasType}");
                        return false;
                    }
                }
                else
                {
                    _aliases.Add(alias, type);
                }
            }
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
