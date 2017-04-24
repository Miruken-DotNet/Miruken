namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class FilterAttribute : Attribute, IFilterProvider
    {
        private static readonly ConcurrentDictionary<MemberInfo, FilterAttribute[]>
            _filters = new ConcurrentDictionary<MemberInfo, FilterAttribute[]>();

        public static readonly FilterAttribute[] NoFilters = new FilterAttribute[0];

        public FilterAttribute(params Type[] filterTypes)
        {
            if (filterTypes == null)
                throw new ArgumentNullException(nameof(filterTypes));
            if (filterTypes.Any(InvalidFilter))
                throw new ArgumentException("All filter types must conform to IFilter<,>");
            FilterTypes = filterTypes;
        }

        public Type[] FilterTypes { get; }
        public bool   Many        { get; set; }
        public int?   Order       { get; set; }

        public IEnumerable<IFilter> GetFilters(IHandler composer)
        {
            var filters = FilterTypes.SelectMany(
                filterType => Many
                    ? composer.ResolveAll(filterType)
                    : new[] {composer.Resolve(filterType)})
                .OfType<IFilter>();

            var relativeOrder = Order;
            foreach (var filter in filters)
            {
                filter.Order = relativeOrder.HasValue ? relativeOrder++ : null;
                yield return filter;
            }
        }

        public static FilterAttribute[] GetFilters(MemberInfo member, bool inherit = false)
        {
            return _filters.GetOrAdd(member, t => Normalize(
                    (FilterAttribute[])member.GetCustomAttributes(
                        typeof(FilterAttribute), inherit)));
        }

        private static FilterAttribute[] Normalize(FilterAttribute[] filters)
        {
            return filters.Length > 0 ? filters : NoFilters;
        }

        private static bool InvalidFilter(Type filterType)
        {
            var anyFilter = typeof(IFilter<,>);
            return filterType == null || (filterType != anyFilter &&
                   filterType.GetInterface(anyFilter.FullName) == null);
        }
    }
}
