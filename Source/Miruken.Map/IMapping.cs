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
            return (T)mapping.Map(source, typeof(T), format);
        }

        public static Promise<T> MapAsync<T>(this IMapping mapping,
            object source, object format = null)
        {
            return (Promise<T>)mapping.MapAsync(source, typeof(T), format)
                .Coerce(typeof(Promise<T>));
        }

        public static T MapInto<T>(this IMapping mapping, 
            object source, T instance, object format = null)
        {
            return (T)mapping.Map(source, instance, format);
        }

        public static Promise<T> MapIntoAsync<T>(this IMapping mapping,
            object source, T instance, object format = null)
        {
            return (Promise<T>)mapping.MapAsync(source, instance, format)
                .Coerce(typeof(Promise<T>));
        }
    }
}
