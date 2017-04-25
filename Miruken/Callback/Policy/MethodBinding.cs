namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class MethodBinding
    {
        private List<IFilterProvider> _filters;

        protected MethodBinding(MethodDispatch dispatch)
        {
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            Dispatcher = dispatch;
            AddFilters(FilterAttribute.GetFilters(Dispatcher.Method));
        }

        public MethodDispatch Dispatcher { get; }

        public IEnumerable<IFilterProvider> Filters =>
            _filters ?? Enumerable.Empty<IFilterProvider>();

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            if (_filters == null)
                _filters = new List<IFilterProvider>();
            _filters.AddRange(providers.Where(p => p != null));
        }

        public abstract bool Dispatch(
            object target, object callback, IHandler composer);
    }
}
