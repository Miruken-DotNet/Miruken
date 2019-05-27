namespace Miruken.Api
{
    using System.Collections.Generic;
    using System.Linq;

    public static class KeyExtensions
    {
        public static Key<T> Find<T>(this IEnumerable<Key<T>> keys, T id)
        {
            return keys.First(k => Equals(k.Id, id));
        }

        public static Key<T> TryFind<T>(this IEnumerable<Key<T>> keys, T id)
        {
            return keys.FirstOrDefault(k => Equals(k.Id, id));
        }

        public static Key<T> FindOrDefault<T>(this IEnumerable<Key<T>> keys, T id)
        {
            return keys.FirstOrDefault(k => Equals(k.Id, id)) ?? new Key<T>(id);
        }

        public static string FindNameOrDefault<T>(this IEnumerable<IKeyProperties<T>> keyProperties, T id)
        {
            return keyProperties.FirstOrDefault(x => Equals(x.Id, id))?.Name ?? string.Empty;
        }

        public static string FindNameOrDefault<T>(this IEnumerable<Key<T>> keys, T id)
        {
            return keys.FirstOrDefault(k => Equals(k.Id, id))?.Name ?? string.Empty;
        }
    }
}
