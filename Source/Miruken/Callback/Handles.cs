namespace Miruken.Callback
{
    using Policy;

    public class Handles : CategoryAttribute
    {
        public Handles()
        {
        }

        public Handles(object key)
        {
            InKey = key;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static void AddFilters(params IFilterProvider[] providers) =>
            Policy.AddFilters(providers);

        public static readonly CallbackPolicy Policy =
            ContravariantPolicy.Create<Command>(r => r.Callback,
                x => x.MatchCallbackMethod(x.Target)
                      .MatchMethod(x.Callback)
            );
    }
}
