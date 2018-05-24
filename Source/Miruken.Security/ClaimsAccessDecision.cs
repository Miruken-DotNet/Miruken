namespace Miruken.Security
{
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;

    public abstract class ClaimsAccessDecision
        : FilterAttribute, IAccessDecision
    {
        protected ClaimsAccessDecision()
            : base(typeof(AuthorizeFilter<,>))
        {
        }

        Task<bool> IAccessDecision.Allow(MethodBinding method,
            IPrincipal principal, IHandler composer)
        {
            return Allow(method, principal, composer);
        }

        protected abstract Task<bool> Allow(MethodBinding method,
            IPrincipal principal, IHandler composer);

        protected static IEnumerable<Claim> ObtainClaims(
            IPrincipal principal, string claimType)
        {
            var identity = principal.Identity;
            if (identity?.IsAuthenticated == true &&
                identity is ClaimsIdentity claimsIdentity)
            {
                return claimsIdentity.FindAll(claimType);
            }
            throw new AuthenticationException(
                "The principal does not represent an authenticated claims identity.");
        }
    }
}
