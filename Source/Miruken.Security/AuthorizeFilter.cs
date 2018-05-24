namespace Miruken.Security
{
    using System.Security.Principal;
    using Callback;
    using Callback.Policy;

    public class AuthorizeFilter<TCb, TRes> : DynamicFilter<TCb, TRes>
    {
        public AuthorizeFilter()
        {
            Order = Stage.Authorization;
        }

        public TRes Next(TCb callback, Next<TRes> next,
            MethodBinding method, IAccessDecision access,
            IPrincipal principal, IHandler composer)
        {
            var proceed = access.Allow(method, principal, composer)
                .GetAwaiter().GetResult();
            return next(proceed: proceed);
        }
    }
}
