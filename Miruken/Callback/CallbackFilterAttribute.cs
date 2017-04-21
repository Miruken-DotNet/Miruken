namespace Miruken.Callback
{
    using System;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public abstract class CallbackFilterAttribute 
        : Attribute, IComparable<CallbackFilterAttribute>
    {
        protected CallbackFilterAttribute(Type filterType)
        {
            if (filterType == null)
                throw new ArgumentNullException(nameof(filterType));
            if (!typeof(ICallbackFilter).IsAssignableFrom(filterType))
                throw new ArgumentException($"{filterType.FullName} must implement ICallbackFilter");
            FilterType = filterType;
        }

        public Type FilterType { get; }

        public int? Order      { get; set; }

        public int CompareTo(CallbackFilterAttribute other)
        {
            if (Order == other.Order) return 0;
            if (Order == null) return 1;
            if (other.Order == null) return -1;
            return Order.Value - other.Order.Value;
        }
    }
}
