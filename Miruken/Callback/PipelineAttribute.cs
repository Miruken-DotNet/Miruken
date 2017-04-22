namespace Miruken.Callback
{
    using System;
    using System.Linq;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class PipelineAttribute : Attribute
    {
        public PipelineAttribute(params Type[] filterTypes)
        {
            if (filterTypes.Any(InvalidPipelineFilter))
                throw new ArgumentException("All filter types must be instantiable IPipelineFilter<,>");
            FilterTypes = filterTypes;
        }

        public Type[] FilterTypes { get; }

        public bool   Many        { get; set; }

        private static bool InvalidPipelineFilter(Type filterType)
        {
            return filterType == null     ||
                   filterType.IsInterface || 
                   filterType.IsAbstract  ||
                   filterType.GetInterface(typeof(IPieplineFilter<,>).FullName) == null;
        }
    }
}
