namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Policy;
    using Policy.Bindings;

    public static class FilterExtensions
    {
        public static IHandler SkipFilters(
            this IHandler handler, bool skip = true)
        {
            return handler == null ? null
                : new FilterOptions
                {
                    SkipFilters = skip
                }.Decorate(handler);
        }

        public static IHandler EnableFilters(
            this IHandler handler, bool enable = true)
        {
            return handler == null ? null
                : new FilterOptions
                {
                    SkipFilters = !enable
                }.Decorate(handler);
        }

        public static IHandler WithFilters(
            this IHandler handler, params IFilter[] filters)
        {
            return handler == null ? null
                 : new FilterOptions
                 {
                     ExtraFilters = new[]
                     {
                         new FilterInstancesProvider(filters)
                     }
                 }.Decorate(handler);
        }

        public static IHandler WithFilters(
            this IHandler handler, params IFilterProvider[] providers)
        {
            return handler == null ? null
                 : new FilterOptions {ExtraFilters = providers}
                    .Decorate(handler);
        }

        public static ICollection<(IFilter, IFilterProvider)>
            GetOrderedFilters(this IHandler handler, MemberBinding binding,
                MemberDispatch dispatcher, object callback, Type callbackType,
                params IEnumerable<IFilterProvider>[] providers)
        {
            var options      = handler.GetOptions<FilterOptions>();
            var allProviders = providers
                .Where(pa => pa != null).SelectMany(pa => pa)
                .Where(p => p?.AppliesTo(callback, callbackType) != false)
                .Concat(options?.ExtraFilters ??
                        Enumerable.Empty<IFilterProvider>());

            switch (options?.SkipFilters)
            {
                case true:
                    allProviders = allProviders.Where(p => p.Required);
                    break;
                case null:
                    if (binding.Dispatcher.SkipFilters)
                        allProviders = allProviders.Where(p => p.Required);
                    handler = handler.SkipFilters();
                    break;
                case false:
                    handler = handler.SkipFilters();
                    break;
            }

            var ordered = new SortedSet<(IFilter, IFilterProvider)>(FilterComparer.Instance);

            foreach (var provider in allProviders)
            {
                var found   = false;
                var filters = provider.GetFilters(
                    binding, dispatcher, callback, callbackType, handler);
                if (filters == null) return null;
                foreach (var filter in filters)
                {
                    if (filter == null) return null;
                    found = true;
                    ordered.Add((filter, provider));
                }
                if (!found) return null;
            }

            return ordered;
        }

        private class FilterComparer : IComparer<(IFilter, IFilterProvider)>
        {
            public static readonly FilterComparer Instance = new();

            public int Compare(
                (IFilter, IFilterProvider) x,
                (IFilter, IFilterProvider) y)
            {
                if (x.Item1 == y.Item1) return 0;
                if (x.Item1.Order == y.Item1.Order || y.Item1.Order == null) return -1;
                if (x.Item1.Order == null) return 1;
                return x.Item1.Order.Value - y.Item1.Order.Value;
            }
        }
    }
}
