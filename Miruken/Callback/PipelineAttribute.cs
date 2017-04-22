namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class PipelineAttribute : Attribute, IPipleineFilterProvider
    {
        public PipelineAttribute(params Type[] filterTypes)
        {
            if (filterTypes == null)
                throw new ArgumentNullException(nameof(filterTypes));
            if (filterTypes.Any(InvalidPipelineFilter))
                throw new ArgumentException("All filter types must be instantiable IPipelineFilter<,>");
            FilterTypes = filterTypes;
        }

        public Type[] FilterTypes { get; }
        public bool   Many        { get; set; }
        public int?   Order       { get; set; }

        IEnumerable<IPipelineFilter> IPipleineFilterProvider.GetPipelineFilters(IHandler composer)
        {
            var filters = FilterTypes.SelectMany(
                filterType => Many
                    ? composer.ResolveAll(filterType)
                    : new[] {composer.Resolve(filterType)})
                .OfType<IPipelineFilter>();

            var relativeOrder = Order;
            foreach (var filter in filters)
            {
                filter.Order = relativeOrder.HasValue ? relativeOrder++ : null;
                yield return filter;
            }
        }

        private static bool InvalidPipelineFilter(Type filterType)
        {
            return filterType == null     ||
                   filterType.IsInterface || 
                   filterType.IsAbstract  ||
                   filterType.GetInterface(typeof(IPieplineFilter<,>).FullName) == null;
        }
    }
}
