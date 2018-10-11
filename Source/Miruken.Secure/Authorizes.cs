namespace Miruken.Secure
{
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Rules;

    public class Authorizes : CategoryAttribute
    {
        public Authorizes()
        {          
        }

        public Authorizes(object policy)
        {
            InKey = policy;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static void AddFilters(params IFilterProvider[] providers) =>
            Policy.AddFilters(providers);

        public static readonly CallbackPolicy Policy =
             ContravariantPolicy.Create<Authorization>(v => v.Target,
                    x => x.MatchCallbackMethod(Return.Of<bool>(), x.Target)
                          .MatchMethod(Return.Of<bool>(), x.Callback)
             );
    }
}
