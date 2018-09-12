namespace Miruken.Infrastructure
{
    using System;
    using System.Collections.Generic;

    public static class CollectionExtensions
    {
        public static void AddSorted<T>(this List<T> list, T item)
            where T : IComparable<T>
        {
            if (list.Count == 0)
            {
                list.Add(item);
                return;
            }
            if (list[list.Count - 1].CompareTo(item) <= 0)
            {
                list.Add(item);
                return;
            }
            if (list[0].CompareTo(item) >= 0)
            {
                list.Insert(0, item);
                return;
            }
            var index = list.BinarySearch(item);
            if (index < 0)
                index = ~index;
            list.Insert(index, item);
        }

        public static void AddSorted<T>(
            this List<T> list, T item, IComparer<T> comparer)
        {
            if (comparer == null)
                throw new ArgumentException(nameof(comparer));
            if (list.Count == 0)
            {
                list.Add(item);
                return;
            }
            if (comparer.Compare(list[list.Count - 1], item) <= 0)
            {
                list.Add(item);
                return;
            }
            if (comparer.Compare(list[0], item) >= 0)
            {
                list.Insert(0, item);
                return;
            }
            var index = list.BinarySearch(item, comparer);
            if (index < 0) index = ~index;
            list.Insert(index, item);
        }
    }
}
