namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class FilterExtensions
    {
        public static FilterOptions GetFilterOptions(this IHandler handler)
        {
            if (handler == null) return null;
            var options = new FilterOptions();
            return handler.Handle(options, true) ? options : null;
        }

        public static IHandler WithFilters(
            this IHandler handler, params IFilter[] filters)
        {
            return handler == null ? null :
                new FilterOptions
                {
                    ExtraFilters = new []
                    {
                        new FilterInstancesProvider(filters)
                    }
                }
                .Decorate(handler);
        }

        public static IHandler WithFilters(
             this IHandler handler, params IFilterProvider[] providers)
        {
            return handler == null ? null :
                new FilterOptions { ExtraFilters = providers }
                .Decorate(handler);
        }

        public static IEnumerable<IFilter> GetFilters(
            this IHandler composer, Type callbackType, Type resultType,
            FilterOptions options, params IEnumerable<IFilterProvider>[] providers)
        {
            if (resultType == typeof(void))
                resultType = typeof(object);
            var extraProviders = options?.ExtraFilters
                ?? Enumerable.Empty<IFilterProvider>();

            foreach (var provider in providers
                .Where(p => p != null).SelectMany(p => p)
                .Concat(extraProviders))
            {
                if (provider == null) continue;
                var filters = provider.GetFilters(
                    callbackType, resultType, composer)
                    .Where(filter => filter != null);
                foreach (var filter in filters)
                    yield return filter;
            }
        }

        public static IEnumerable<IFilter> GetOrderedFilters(
            this IHandler handler, Type callbackType, Type resultType, 
            params IEnumerable<IFilterProvider>[] providers)
        {
            var options = handler.GetFilterOptions();
            return handler.GetFilters(
                callbackType, resultType, options, providers)
                .OrderByDescending(f => f.Order ?? int.MaxValue);
        }
    }
}
