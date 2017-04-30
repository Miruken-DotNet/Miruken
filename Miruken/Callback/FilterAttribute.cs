namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class FilterAttribute : Attribute, IFilterProvider
    {
        private static readonly ConcurrentDictionary<MemberInfo, FilterAttribute[]>
            _filters = new ConcurrentDictionary<MemberInfo, FilterAttribute[]>();

        public static readonly FilterAttribute[] NoFilters = new FilterAttribute[0];

        public FilterAttribute(params Type[] filterTypes)
        {
            ValidateFilters(filterTypes);
            FilterTypes = filterTypes;
        }

        public Type[] FilterTypes { get; }
        public bool   Many        { get; set; }
        public int?   Order       { get; set; }

        public IEnumerable<IFilter> GetFilters(
            Type callbackType, Type resulType, IHandler composer)
        {
            var filters = FilterTypes
                .Select(f => CloseFilterType(f, callbackType, resulType))
                .SelectMany(filterType => Many
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

        private static Type CloseFilterType(
            Type filterType, Type callbackType, Type resulType)
        {
            if (!filterType.IsGenericTypeDefinition)
                return filterType;
            var openFilterType = typeof(IFilter<,>);
            if (filterType == openFilterType)
                return filterType.MakeGenericType(callbackType, resulType);
            var genericArgs  = filterType.GetGenericArguments();
            var conformance  = filterType.GetOpenTypeConformance(openFilterType);
            var inferredArgs = conformance.GetGenericArguments();
            return filterType.MakeGenericType(genericArgs.Select((arg, i) =>
            {
                switch (Array.IndexOf(inferredArgs, arg))
                {
                    case 0: return callbackType;
                    case 1: return resulType;
                }
                throw new InvalidOperationException($"{filterType.FullName} generic arg {i} could not be inferred");   
            }).ToArray());
        }

        private static void ValidateFilters(Type[] filterTypes)
        {
            if (filterTypes == null)
                throw new ArgumentNullException(nameof(filterTypes));
            var anyFilter     = typeof(IFilter<,>);
            var dynamicFilter = typeof(IDynamicFilter);
            foreach (var filterType in filterTypes)
            {
                if (filterType == null)
                    throw new ArgumentException("Filter types cannot be nulll");  
                if (filterType == anyFilter ||
                    dynamicFilter.IsAssignableFrom(filterType)) continue;
                var conformance = filterType.GetOpenTypeConformance(anyFilter);
                if (conformance == null)
                    throw new ArgumentException($"{filterType.FullName} does not conform to IFilter<,>");
                if (filterType.IsGenericTypeDefinition)
                {
                    var genericArgs = filterType.GetGenericArguments();
                    if (genericArgs.Length > 2)
                        throw new ArgumentException($"{filterType.FullName} has {genericArgs.Length} generic args, but only two can be inferred");
                    var inferredArgs = conformance.GetGenericArguments();
                    for (var i = 0; i < genericArgs.Length; ++i)
                    {
                        if (Array.IndexOf(inferredArgs, genericArgs[i]) < 0)
                            throw new ArgumentException($"{filterType.FullName} generic arg {i} cannot be inferred");
                    }
                }
            }
        }
    }
}
