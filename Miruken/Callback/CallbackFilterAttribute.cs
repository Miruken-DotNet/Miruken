namespace Miruken.Callback
{
    using System;
    using System.Linq;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class CallbackFilterAttribute : Attribute
    {
        public CallbackFilterAttribute(params Type[] filterTypes)
        {
            if (filterTypes.Any(InvalidCallbackFilterType))
                throw new ArgumentException("All filter types must implement ICallbackFilter<,>");
            FilterTypes = filterTypes;
        }

        public Type[] FilterTypes { get; }

        public bool   Many        { get; set; }

        private static bool InvalidCallbackFilterType(Type filterType)
        {
            return filterType == null     ||
                   filterType.IsInterface || 
                   filterType.IsAbstract  ||
                   filterType.GetInterface(typeof(ICallbackFilter<,>).FullName) == null;
        }
    }
}
