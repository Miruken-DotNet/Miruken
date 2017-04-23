namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public delegate Res FilterDelegate<out Res>(bool proceed = true);

    public interface IFilter
    {
        int? Order { get; set; }
    }

    public interface IFilter<in Cb, Res> : IFilter
    {
        Res Filter(Cb callback, IHandler composer, FilterDelegate<Res> proceed);
    }

    public interface IFilterProvider
    {
        IEnumerable<IFilter> GetFilters(IHandler composer);
    }

    public class FilterOptions : Options<FilterOptions>
    {
        public bool?             SuppressFilters   { get; set; }
        public Type[]            SuppressedFilters { get; set; }
        public IFilterProvider[] AdditionalFilters { get; set; }

        public override void MergeInto(FilterOptions other)
        {
            if (SuppressFilters.HasValue && !other.SuppressFilters.HasValue)
                other.SuppressFilters = SuppressFilters;

            if (SuppressedFilters != null)
            {
                other.SuppressedFilters = other.SuppressedFilters?
                    .Concat(SuppressedFilters).ToArray() ?? SuppressedFilters;
            }

            if (AdditionalFilters != null)
            {
                other.AdditionalFilters = other.AdditionalFilters?
                    .Concat(AdditionalFilters).ToArray() ?? AdditionalFilters;
            }
        }
    }

    public static class FilterHelper
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
             this IHandler handler, params IFilterProvider[] filters)
        {
            return handler == null ? null :
                new FilterOptions { AdditionalFilters = filters }
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
            this IHandler composer, Type[] suppress,
            params IEnumerable<IFilterProvider>[] providers)
        {
            if (providers == null || providers.Length == 0)
                yield break;

            foreach (var provider in providers
                .Where(p => p != null).SelectMany(p => p))
            {
                if (provider == null) continue;
                var filters = provider.GetFilters(composer);
                if (suppress != null && suppress.Length > 0)
                    filters = filters.Where(filter =>
                        !suppress.Any(type => filter.GetType().IsClassOf(type)));
                foreach (var filter in filters)
                    yield return filter;
            }
        }

        public static IEnumerable<IFilter> GetOrderedFilters(
            this IHandler composer, Type[] suppress,
            params IEnumerable<IFilterProvider>[] providers)
        {
            return composer.GetFilters(suppress, providers)
                .OrderByDescending(f => f.Order ?? int.MaxValue);
        }

        public static IHandler ResolveOpenFilters(
            this IHandler handler, Type callbackType, Type resultType)
        {
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
    }
}
