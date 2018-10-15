namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Infrastructure;
    using Policy;
    using Policy.Bindings;

    [AttributeUsage(AttributeTargets.Class |
        AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = true, Inherited = false),
    DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class FilterAttribute : Attribute, IFilterProvider
    {
        public FilterAttribute(Type filterType)
        {
            ValidateFilterConformance(filterType);
            FilterType = filterType;
        }

        public Type FilterType { get; }
        public int? Order      { get; set; }
        public bool Required   { get; set; }

        public IEnumerable<IFilter> GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            Type callbackType, IHandler composer)
        {
            var closedFilterType = dispatcher.CloseFilterType(FilterType, callbackType);
            if (!AcceptFilterType(closedFilterType, binding))
                return Enumerable.Empty<IFilter>();

            var filter = (IFilter)composer.Resolve(closedFilterType);
            if (filter == null) return Enumerable.Empty<IFilter>();
            if (Order.HasValue) filter.Order = Order.Value;

            return new[] { filter };
        }

        protected virtual void ValidateFilterType(Type filterType)
        {          
        }

        protected virtual bool AcceptFilterType(Type filterType, MemberBinding binding)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            return obj is FilterAttribute other
                && other.FilterType == FilterType
                && other.Order == Order
                && other.Required == Required;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (FilterType.GetHashCode() * 31 
                     + (Order?.GetHashCode() ?? 0)) * 31
                     +  Required.GetHashCode();
            }
        }

        private void ValidateFilterConformance(Type filterType)
        {
            if (filterType == null)
                throw new ArgumentNullException(nameof(filterType));
            if (filterType == null)
                throw new ArgumentException("Filter types cannot be null");
            if (filterType == typeof(IFilter<,>)) 
                throw new ArgumentException("Filter type cannot be unspecified");
            var conformance = filterType.GetOpenTypeConformance(typeof(IFilter<,>));
            if (conformance == null)
                throw new ArgumentException($"{filterType.FullName} does not conform to IFilter<,>");
            if (filterType.IsGenericTypeDefinition && !conformance.ContainsGenericParameters)
                throw new ArgumentException($"{filterType.FullName} generic args cannot be inferred");
            ValidateFilterType(filterType);
        }

        private string DebuggerDisplay
        {
            get
            {
                var order    = Order.HasValue ? ", Order = " + Order : "";
                var required = Required ? ", Required" : "";
                return $"{FilterType.FullName}{order}{required}";
            }
        }
    }
}
