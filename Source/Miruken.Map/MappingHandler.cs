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
            return Composer.Handle(mapFrom) 
                 ? mapFrom.Result 
                 : Unhandled<object>();
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
            try
            {
                return Composer.Handle(mapFrom)
                     ? (Promise)mapFrom.Result
                     : Unhandled<Promise>();
            }
            catch (Exception ex)
            {
                return Promise.Rejected(ex);
            }
        }
    }
}
