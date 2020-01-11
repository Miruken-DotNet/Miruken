namespace Miruken.Callback
{
    using System;
    using Policy;

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    public class Creates : CategoryAttribute
    {
        public override CallbackPolicy CallbackPolicy => Policy;

        public static void AddFilters(params IFilterProvider[] providers) =>
            Policy.AddFilters(providers);

        public static readonly CallbackPolicy Policy =
             CovariantPolicy.Create<Creation>(c => c.Type,
                 x => x.MatchMethod(x.ReturnKey.OrVoid, x.Callback)
                     .MatchMethod(x.ReturnKey)
             );
    }
}
