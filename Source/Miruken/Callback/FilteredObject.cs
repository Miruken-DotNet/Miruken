namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;

    public abstract class FilteredObject : IFiltered
    {
        private HashSet<IFilterProvider> _filters;
        
        public IEnumerable<IFilterProvider> Filters
        {
            get
            {
                if (_filters != null) return _filters;
                return Array.Empty<IFilterProvider>();
            }
        }

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            if (_filters == null)
                _filters = new HashSet<IFilterProvider>(providers);
            else 
                Array.ForEach(providers, p => _filters.Add(p));
        }

        public void RemoveFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            if (_filters != null)
                Array.ForEach(providers, p => _filters.Remove(p));
        }

        public void RemoveAllFilters()
        {
            _filters?.Clear();
        }
    }
}
