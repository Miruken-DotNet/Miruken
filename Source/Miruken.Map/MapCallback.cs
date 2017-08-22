namespace Miruken.Map
{
    public class MapCallback
    {
        public object Format { get; set; }
    }

    public class MapFrom : MapCallback
    {
        public MapFrom(object source)
        {
            Source = source;
        }

        public object Source { get; }
    }

    public class MapTo : MapCallback
    {
        public MapTo(object formattedValue, object typeOrInstance)
        {
            FormattedValue = formattedValue;
            TypeOrInstance = typeOrInstance;
        }

        public object FormattedValue { get; }

        public object TypeOrInstance { get; }
    }
}
