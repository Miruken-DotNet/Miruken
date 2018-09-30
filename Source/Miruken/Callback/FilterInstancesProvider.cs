namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using Policy;
    using Policy.Bindings;

    public class FilterInstancesProvider : IFilterProvider
    {
        private readonly IFilter[] _filters;

        public FilterInstancesProvider(params IFilter[] filters)
        {
            _filters = filters;
        }

        public FilterInstancesProvider(
            bool required, params IFilter[] filters)
            : this(filters)
        {
            Required = required;
        }

        public bool Required { get; }

        public IEnumerable<IFilter> GetFilters(MemberBinding binding,
            Type callbackType, Type logicalResultType, IHandler composer)
        {
            return _filters;
        }
    }
}