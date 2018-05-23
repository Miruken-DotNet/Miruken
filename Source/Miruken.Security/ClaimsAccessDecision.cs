namespace Miruken.Security
{
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Security.Claims;
    using System.Security.Principal;
    using Callback;

    public abstract class ClaimsAccessDecision
        : FilterAttribute, IAccessDecision
    {
        protected ClaimsAccessDecision()
            : base(typeof(AuthorizeFilter<,>))
        {
        }

        bool IAccessDecision.Allow(IPrincipal principal)
        {
            return Allow(principal);
        }

        protected abstract bool Allow(IPrincipal principal);

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
                "The current principal does not have an authenticated ClaimsIdentity");
        }
    }
}
