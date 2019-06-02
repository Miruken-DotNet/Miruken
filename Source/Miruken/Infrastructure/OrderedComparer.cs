namespace Miruken.Infrastructure
{
    using System.Collections.Generic;

    public class OrderedComparer<T> : IComparer<T>
    {
        public static readonly OrderedComparer<T> Instance = new OrderedComparer<T>();

        private OrderedComparer()
        {
        }

        public int Compare(T x, T y)
        {
            var order1 = (x as IOrdered)?.Order;
            var order2 = (y as IOrdered)?.Order;
            if (order1 == order2) return 0;

            if (order1 == null) return 1;
            if (order2 == null) return -1;

            if (order1 == int.MinValue) return -1;
            if (order2 == int.MinValue) return 1;

            return order1.Value - order2.Value;
        }
    }
}
