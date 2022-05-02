namespace Miruken.Callback.Policy.Rules;

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

        if (_aliases.TryGetValue(alias, out var aliasedType))
        {
            if (aliasedType == type) return true;
            AddError("Mismatched alias '{alias}', {type} != {aliasType}");
            return false;
        }
        _aliases.Add(alias, type);
        return true;
    }

    public void AddError(string error)
    {
        _errors ??= new List<string>();
        _errors.Add(error);
    }
}