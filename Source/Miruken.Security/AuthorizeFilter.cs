namespace Miruken.Security
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;

    public class AuthorizeFilter<TCb, TRes> : DynamicFilter<TCb, TRes>
    {
        public AuthorizeFilter()
        {
            Order = Stage.Authorization;
        }

        public async Task<TRes> Next(
            TCb callback, Next<TRes> next,
            MethodBinding method, IAccessDecision access,
            IPrincipal principal, IHandler composer,
            IFilterProvider provider)
        {
            if (await access.CanAccess(
                method, principal, provider, composer))
                return await next();

            throw new UnauthorizedAccessException(
                "Authorization has been denied");
        }
    }
}
