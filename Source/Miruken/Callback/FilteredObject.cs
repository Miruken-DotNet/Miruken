namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;

    public abstract class FilteredObject : IFiltered
    {
        private HashSet<IFilterProvider> _filters;

        protected FilteredObject()
        {
        }

        public IEnumerable<IFilterProvider> Filters => _filters;

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            if (_filters == null)
                _filters = new HashSet<IFilterProvider>(providers);
            else 
                Array.ForEach(providers, p => _filters.Add(p));
        }
    }
}
