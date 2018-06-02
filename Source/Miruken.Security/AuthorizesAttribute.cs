namespace Miruken.Security
{
    using Callback;
    using Callback.Policy;

    public class AuthorizesAttribute : CategoryAttribute
    {
        public AuthorizesAttribute()
        {          
        }

        public AuthorizesAttribute(object policy)
        {
            InKey = policy;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static readonly CallbackPolicy Policy =
             ContravariantPolicy.Create<Authorization>(v => v.Target,
                    x => x.MatchCallbackMethod(
                            Return.Of<bool>(), x.Target, x.Extract(v => v.Principal))
                          .MatchMethod(Return.Of<bool>(), x.Target, x.Callback)
                          .MatchMethod(Return.Of<bool>(), x.Callback)
             );
    }
}
