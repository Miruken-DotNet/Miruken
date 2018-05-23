namespace Miruken.Security
{
    using System.Security.Principal;
    using Callback;
    using Callback.Policy;

    public class AuthorizeFilter<TCb, TRes> : DynamicFilter<TCb, TRes>
    {
        public TRes Next(TCb callback, Next<TRes> next,
            MethodBinding method, IAccessDecision access,
            IPrincipal principal, IHandler composer)
        {
            return next(proceed: access.Allow(principal, composer));
        }
    }
}
