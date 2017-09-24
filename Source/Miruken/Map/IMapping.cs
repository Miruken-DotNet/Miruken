namespace Miruken.Map
{
    using Concurrency;

    public interface IMapping
    {
        object Map(object source, object typeOrInstance, object format = null);

        Promise MapAsync(object source, object typeOrInstance, object format = null);
    }

    public static class MappingExtensions
    {
        public static T Map<T>(this IMapping mapping,
            object source, object format = null)
        {
            var result = mapping.Map(source, typeof(T), format);
            return result == null ? default(T) : (T)result;
        }

        public static Promise<T> MapAsync<T>(this IMapping mapping,
            object source, object format = null)
        {
            var result = mapping.MapAsync(source, typeof(T), format);
            return result == null ? Promise.Resolved(default(T))
                 : (Promise<T>)result.Coerce(typeof(Promise<T>));
        }

        public static T MapInto<T>(this IMapping mapping, 
            object source, T instance, object format = null)
        {
            var result = mapping.Map(source, instance, format);
            return result == null ? default(T) : (T)result;
        }

        public static Promise<T> MapIntoAsync<T>(this IMapping mapping,
            object source, T instance, object format = null)
        {
            var result = mapping.MapAsync(source, instance, format);
            return result == null ? Promise.Resolved(default(T))
                 : (Promise<T>)result.Coerce(typeof(Promise<T>));
        }
    }
}
