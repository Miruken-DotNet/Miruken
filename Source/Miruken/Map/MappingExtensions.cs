namespace Miruken.Map
{
    using System;
    using System.Collections;
    using System.Linq;
    using Callback;
    using Concurrency;
    using Infrastructure;

    public static class MappingExtensions
    {
        public static object Map(this IHandler handler,
            object source, object typeOrInstance, object format = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (typeOrInstance == null)
                throw new ArgumentNullException(nameof(typeOrInstance));
            var mapping = new Mapping(source, typeOrInstance, format);
            if (!handler.Handle(mapping))
                throw new NotSupportedException($"Mapping {mapping} not handled");
            return mapping.Result;
        }

        public static Promise MapAsync(this IHandler handler,
            object source, object typeOrInstance, object format = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (typeOrInstance == null)
                throw new ArgumentNullException(nameof(typeOrInstance));
            var mapping = new Mapping(source, typeOrInstance, format)
            {
                WantsAsync = true
            };
            if (!handler.Handle(mapping))
                throw new NotSupportedException($"Mapping {mapping} not handled");
            return (Promise) mapping.Result;
        }

        public static T Map<T>(this IHandler handler,
            object source, object format = null)
        {
            var result = handler.Map(source, typeof(T), format);
            return result == null ? default(T) : (T)result;
        }

        public static Promise<T> MapAsync<T>(this IHandler handler,
            object source, object format = null)
        {
            var result = handler.MapAsync(source, typeof(T), format);
            return result == null ? Promise.Resolved(default(T))
                 : (Promise<T>)result.Coerce(typeof(Promise<T>));
        }

        public static T[] MapAll<T>(this IHandler handler,
            IEnumerable source, object format = null)
        {
            return source.Cast<object>()
                .Select(s => handler.Map<T>(s, format))
                .ToArray();
        }

        public static Promise<T[]> MapAllAsync<T>(this IHandler handler,
            IEnumerable source, object format = null)
        {
            return Promise.All(source.Cast<object>()
                    .Select(s => handler.MapAsync<T>(s, format)))
                .Then((r, s) => r.Cast<T>().ToArray());
        }

        public static T MapInto<T>(this IHandler handler,
            object source, T instance, object format = null)
        {
            var result = handler.Map(source, instance, format);
            return result == null ? default(T) : (T)result;
        }

        public static Promise<T> MapIntoAsync<T>(this IHandler handler,
            object source, T instance, object format = null)
        {
            var result = handler.MapAsync(source, instance, format);
            return result == null ? Promise.Resolved(default(T))
                 : (Promise<T>)result.Coerce(typeof(Promise<T>));
        }
    }
}
