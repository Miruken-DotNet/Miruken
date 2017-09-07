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
            var mapFrom = new MapFrom(source, format);
            return Composer.Handle(mapFrom) ? mapFrom.Result : null;
        }

        public object MapTo(object formattedValue, object format, object typeOrInstance)
        {
            if (formattedValue == null)
                throw new ArgumentNullException(nameof(formattedValue));
            var mapTo = new MapTo(formattedValue, typeOrInstance, format);
            return Composer.Handle(mapTo) ? mapTo.Result : null;
        }
    }
}
