namespace Miruken.Map
{
    public abstract class MapCallback
    {
        protected MapCallback(object format)
        {
            Format = format;
        }

        public object Format  { get; }
        public object Mapping { get; set; }
    }

    public class MapFrom : MapCallback
    {
        public MapFrom(object source, object format)
            : base(format)
        {
            Source = source;
        }

        public object Source { get; }
    }

    public class MapTo : MapCallback
    {
        public MapTo(object formattedValue, object typeOrInstance,
                     object format)
            : base(format)
        {
            FormattedValue = formattedValue;
            TypeOrInstance = typeOrInstance;
        }

        public object FormattedValue { get; }
        public object TypeOrInstance { get; }
    }
}
