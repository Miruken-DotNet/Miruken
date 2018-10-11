namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public abstract class FilteredObject : IFiltered
    {
        private IFilterProvider[] _filters = Array.Empty<IFilterProvider>();

        protected FilteredObject()
        {
        }

        public IEnumerable<IFilterProvider> Filters => _filters;

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            _filters = _filters.Concat(providers.Where(p => p != null))
                .ToArray().Normalize();
        }
    }
}
