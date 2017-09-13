namespace Miruken.Map
{
    using System;
    using Callback;
    using Concurrency;

    public class MappingHandler : Handler, IMapping
    {
        public object Map(object source, object typeOrInstance,
                          object format = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (typeOrInstance == null)
                throw new ArgumentNullException(nameof(typeOrInstance));
            var mapFrom = new Mapping(source, typeOrInstance, format);
            if (!Composer.Handle(mapFrom))
                throw new InvalidOperationException(
                    $"Mapping not found from {source} to {format}");
            return mapFrom.Result;
        }

        public Promise MapAsync(object source, object typeOrInstance,
                                object format)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (typeOrInstance == null)
                throw new ArgumentNullException(nameof(typeOrInstance));
            var mapFrom = new Mapping(source, typeOrInstance, format)
            {
                WantsAsync = true
            };
            if (!Composer.Handle(mapFrom))
                return Promise.Rejected(
                    new InvalidOperationException(
                         $"Mapping not found from {source} to {format}"));
            return (Promise)mapFrom.Result;
        }
    }
}
