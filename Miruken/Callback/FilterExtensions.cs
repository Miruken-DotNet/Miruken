namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public static class FilterExtensions
    {
        public static FilterOptions GetFilterOptions(this IHandler handler)
        {
            if (handler == null) return null;
            var options = new FilterOptions();
            return handler.Handle(options, true) ? options : null;
        }

        public static IHandler SuppressFilters(this IHandler handler)
        {
            return handler == null ? null :
                 new FilterOptions { SuppressFilters = true }
                 .Decorate(handler);
        }

        public static IHandler WithFilters(
            this IHandler handler, params IFilter[] filters)
        {
            return handler == null ? null :
                new FilterOptions { ExtraFilters = filters }
                .Decorate(handler);
        }

        public static IHandler WithFilters(
             this IHandler handler, params IFilterProvider[] providers)
        {
            return handler == null ? null :
                new FilterOptions { ExtraProviders = providers }
                .Decorate(handler);
        }

        public static IHandler WithoutFilters(
            this IHandler handler, params Type[] filterTypes)
        {
            return handler == null ? null :
                new FilterOptions { SuppressedFilters = filterTypes }
                .Decorate(handler);
        }

        public static IEnumerable<IFilter> GetFilters(
            this IHandler composer, FilterOptions options,
            params IEnumerable<IFilterProvider>[] providers)
        {
            if (options == null && providers.Length == 0)
                yield break;

            var suppress       = options?.SuppressedFilters;
            var extraProviders = options?.ExtraProviders
                              ?? Enumerable.Empty<IFilterProvider>();

            composer = composer.SuppressFilters();

            foreach (var provider in providers
                .Where(p => p != null).SelectMany(p => p)
                .Concat(extraProviders))
            {
                if (provider == null) continue;
                var filters = provider.GetFilters(composer)
                    .Where(f => AcceptFilter(suppress, f));
                foreach (var filter in filters)
                    yield return filter;
            }

            var extraFilters = options?.ExtraFilters
                .Where(f => AcceptFilter(suppress, f));
            if (extraFilters != null)
                foreach (var filter in extraFilters)
                    yield return filter;

        }

        public static IEnumerable<IFilter> GetOrderedFilters(
            this IHandler composer, FilterOptions options,
            params IEnumerable<IFilterProvider>[] providers)
        {
            return composer.GetFilters(options, providers)
                .OrderByDescending(f => f.Order ?? int.MaxValue);
        }

        public static IHandler ResolveOpenFilters(
            this IHandler handler, Type callbackType, Type resultType)
        {
            if (resultType == typeof(void))
                resultType = typeof(object);

            return handler.Provide((resolution, composer) =>
            {
                var filterType = resolution.Key as Type;
                if (filterType?.IsGenericTypeDefinition != true ||
                    !typeof(IFilter).IsAssignableFrom(filterType))
                    return false;
                filterType = filterType.MakeGenericType(callbackType, resultType);
                if (resolution.Many)
                {
                    var filters = composer.ResolveAll(filterType);
                    if (filters == null || filters.Length == 0)
                        return false;
                    foreach (var filter in filters)
                        resolution.Resolve(filter, composer);
                }
                else
                {
                    var filter = composer.Resolve(filterType);
                    if (filter == null) return false;
                    resolution.Resolve(filter, composer);
                }
                return true;
            });
        }

        private static bool AcceptFilter(Type[] suppress, IFilter filter)
        {
            if (filter == null) return false;
            if (suppress == null || suppress.Length == 0) return true;
            return !suppress.Any(type => filter.GetType().IsClassOf(type));
        }
    }
}
