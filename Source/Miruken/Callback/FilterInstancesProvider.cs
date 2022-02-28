namespace Miruken.Callback;

using System;
using System.Collections.Generic;
using Policy;
using Policy.Bindings;

public class FilterInstancesProvider : IFilterProvider
{
    private readonly HashSet<IFilter> _filters;

    public FilterInstancesProvider(params IFilter[] filters)
    {
        if (filters.Length == 0)
            throw new ArgumentException("At least one filter must be provided.");
        _filters = new HashSet<IFilter>(filters);
    }

    public FilterInstancesProvider(
        bool required, params IFilter[] filters)
        : this(filters)
    {
        Required = required;
    }

    public bool Required { get; }

    public bool? AppliesTo(object callback, Type callbackType) => null;

    public IEnumerable<IFilter> GetFilters(
        MemberBinding binding, MemberDispatch dispatcher,
        object callback, Type callbackType, IHandler composer)
    {
        return _filters;
    }
}