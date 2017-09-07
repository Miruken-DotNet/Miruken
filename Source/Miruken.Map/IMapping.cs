namespace Miruken.Map
{
    using Concurrency;

    public interface IMapping
    {
        object Map(object source, object format, object typeOrInstance);

        Promise MapAsync(object source, object format, object typeOrInstance);
    }

    public static class MappingExtensions
    {
        public static T Map<T>(
            this IMapping mapping, object source)
        {
            return (T)mapping.Map(source, typeof(T), null);
        }

        public static Promise<T> MapAsync<T>(
            this IMapping mapping, object source)
        {
            return (Promise<T>)mapping.MapAsync(source, typeof(T), null)
                .Coerce(typeof(Promise<T>));
        }

        public static T Map<T>(
            this IMapping mapping, object source, T instance)
        {
            return (T)mapping.Map(source, typeof(T), instance);
        }

        public static Promise<T> MapAsync<T>(
            this IMapping mapping, object source, T instance)
        {
            return (Promise<T>)mapping.MapAsync(source, typeof(T), instance)
                .Coerce(typeof(Promise<T>));
        }
    }
}
