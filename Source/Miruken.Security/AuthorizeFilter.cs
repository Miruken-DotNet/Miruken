namespace Miruken.Security
{
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;

    public class AuthorizeFilter<TCb, TRes> : DynamicFilter<TCb, Task<TRes>>
    {
        public AuthorizeFilter()
        {
            Order = Stage.Authorization;
        }

        public async Task<TRes> Next(TCb callback, Next<Task<TRes>> next,
            MethodBinding method, IAccessDecision access,
            IPrincipal principal, IHandler composer)
        {
            var allowed = await access.Allow(method, principal, composer);
            return await next(proceed: allowed);
        }
    }
}
