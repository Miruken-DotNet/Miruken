namespace Miruken.Infrastructure
{
    using System;
    using System.Collections.Generic;

    public class WeightedComparer<T> : IComparer<Tuple<T, int>>
    {
        public static readonly WeightedComparer<T>
            Instance = new WeightedComparer<T>();

        private WeightedComparer()
        {         
        }

        public int Compare(Tuple<T, int> x, Tuple<T, int> y)
        {
            if (Equals(x, y)) return 0;
            if (x == null) return 1;
            if (y == null) return -1;
            var weight1 = x.Item2;
            var weight2 = y.Item2;
            return weight1 == weight2 ? 1 : weight1 - weight2;
        }
    }
}
