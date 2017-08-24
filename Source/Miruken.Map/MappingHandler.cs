namespace Miruken.Map
{
    using System;
    using Callback;

    public class MappingHandler : Handler, IMapping
    {
        public object MapFrom(object source, object format)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            var composer = Composer;
            var mapFrom  = new MapFrom(source, format);
            return composer.Handle(mapFrom) ? mapFrom.Mapping : null;
        }

        public object MapTo(object formattedValue, object format, object typeOrInstance)
        {
            if (formattedValue == null)
                throw new ArgumentNullException(nameof(formattedValue));
            return null;
        }
    }
}
