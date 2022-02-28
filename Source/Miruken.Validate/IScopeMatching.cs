namespace Miruken.Validate;

using System;
using System.Linq;

public interface IScopeMatching
{
    bool Matches(object scope);
}

public class EqualsScopeMatcher : IScopeMatching
{
    private readonly object _scope;

    public static readonly EqualsScopeMatcher
        Default = new(Scopes.Default);

    public EqualsScopeMatcher(object scope)
    {
        _scope = scope;
    }

    public bool Matches(object scope)
    {
        if (Equals(scope, Scopes.Any) ||
            Equals(_scope, Scopes.Any))
            return true;
        return scope is object[] collection
            ? Array.IndexOf(collection, _scope) >= 0 
            : Equals(scope, _scope);
    }
}

public class CompositeScopeMatcher : IScopeMatching
{
    private readonly IScopeMatching[] _matchers;

    public CompositeScopeMatcher(IScopeMatching[] matchers)
    {
        _matchers = matchers;
    }

    public bool Matches(object scope)
    {
        return _matchers.Any(matcher => matcher.Matches(scope));
    }
}