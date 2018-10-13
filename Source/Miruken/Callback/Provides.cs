namespace Miruken.Callback
{
    using System;
    using Policy;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property |
                    AttributeTargets.Constructor,
        AllowMultiple = true, Inherited = false)]
    public class Provides : CategoryAttribute
    {
        public Provides()
        {
        }

        public Provides(object key)
        {
            OutKey = key;
        }

        public Provides(string key, StringComparison comparison)
        {
            OutKey = new StringKey(key, comparison);
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static void AddFilters(params IFilterProvider[] providers) =>
            Policy.AddFilters(providers);

        public static readonly CallbackPolicy Policy =
             CovariantPolicy.Create<Inquiry>(r => r.Key,
                x => x.MatchMethod(x.ReturnKey.OrVoid, x.Callback)
                      .MatchMethod(x.ReturnKey)
                );
    }
}
