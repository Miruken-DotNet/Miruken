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

        public static IEnumerable<(IFilter, IFilterProvider)>
            GetOrderedFilters(this IHandler handler, MemberBinding binding,
                MemberDispatch dispatcher, Type callbackType,
                params IEnumerable<IFilterProvider>[] providers)
        {
            var options = handler.GetOptions<FilterOptions>();
            switch (options?.SkipFilters)
            {
                case true:
                    return providers.Any(pa => pa?.Any(p => p.Required) == true)
                         ? null : Array.Empty<(IFilter, IFilterProvider)>();
                case null:
                    if (binding.Dispatcher.SkipFilters)
                        return Array.Empty<(IFilter, IFilterProvider)>();
                    handler = handler.SkipFilters();
                    break;
            }
            return handler.GetOrderedFilters(binding, dispatcher,
                callbackType, options, providers);
        }

        private static IEnumerable<(IFilter, IFilterProvider)>
            GetOrderedFilters(this IHandler handler,
                MemberBinding binding, MemberDispatch dispatcher,
                Type callbackType, FilterOptions options,
                params IEnumerable<IFilterProvider>[] providers)
        {
            return new SortedSet<(IFilter, IFilterProvider)>(providers
                    .Where(p => p != null)
                    .SelectMany(p => p)
                    .Concat(options?.ExtraFilters ??
                            Enumerable.Empty<IFilterProvider>())
                    .Where(provider => provider != null)
                    .SelectMany(provider => provider
                        .GetFilters(binding, dispatcher, callbackType, handler)
                        .Where(filter => filter != null)
                        .Select(filter => (filter, provider))),
                FilterComparer.Instance);
        }

        private class FilterComparer : IComparer<(IFilter, IFilterProvider)>
        {
            public static readonly FilterComparer Instance = new FilterComparer();

            public int Compare(
                (IFilter, IFilterProvider) x,
                (IFilter, IFilterProvider) y)
            {
                if (x.Item1 == y.Item1) return 0;
                if (y.Item1?.Order == null) return -1;
                if (x.Item1?.Order == null) return 1;
                return x.Item1.Order.Value - y.Item1.Order.Value;
            }
        }
    }
}
