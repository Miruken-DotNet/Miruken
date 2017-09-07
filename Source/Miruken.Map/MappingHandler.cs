namespace Miruken.Map
{
    using System;
    using Callback;
    using Concurrency;

    public class MappingHandler : Handler, IMapping
    {
        public object Map(object source, object format, object typeOrInstance)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            var mapFrom = new MapFrom(source, format, typeOrInstance);
            if (!Composer.Handle(mapFrom))
                throw new InvalidOperationException("Mapping not found");
            return mapFrom.Result;
        }

        public Promise MapAsync(object source, object format, object typeOrInstance)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            var mapFrom = new MapFrom(source, format, typeOrInstance)
            {
                WantsAsync = true
            };
            if (!Composer.Handle(mapFrom))
                return Promise.Rejected(
                    new InvalidOperationException("Mapping not found"));
            return (Promise)mapFrom.Result;
        }
    }
}
