namespace Miruken.Infrastructure
{
    using System;
    using System.Collections.Concurrent;

    public static class ConcurrentDictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dict,
            TKey key, Func<TKey, TValue> generator,
            out bool added)
        {
            while (true)
            {
                TValue value;
                if (dict.TryGetValue(key, out value))
                {
                    added = false;
                    return value;
                }

                value = generator(key);
                if (dict.TryAdd(key, value))
                {
                    added = true;
                    return value;
                }
            }
        }
    }
}
