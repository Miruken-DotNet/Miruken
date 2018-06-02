namespace Miruken.Security
{
    using System;
    using System.Linq;
    using System.Security.Authentication;
    using System.Security.Claims;
    using System.Security.Principal;

    public static class PrincipalExtensions
    {
        public static ClaimsIdentity RequireClaims(
            this IPrincipal principal)
        {
            var identity = principal.Identity;
            if (identity is ClaimsIdentity claimsIdentity)
                return claimsIdentity;

            throw new AuthenticationException(
                "The principal is not authenticated");
        }

        public static ClaimsIdentity RequireAuthenticatedClaims(
            this IPrincipal principal)
        {
            var identity = principal.Identity;
            if (identity?.IsAuthenticated == true &&
                identity is ClaimsIdentity claimsIdentity)
                return claimsIdentity;

            throw new AuthenticationException(
                    "The principal is not authenticated"); 
        }

        public static bool HasClaim(this ClaimsIdentity identity,
            string type, string value = null)
        {
            return identity.FindAll(type).Any(claim =>
                string.IsNullOrEmpty(value) || claim.Value == value);
        }

        public static bool HasRole(this ClaimsIdentity identity, string role)
        {
            if (string.IsNullOrEmpty(role))
                throw new ArgumentException(@"Role must be specified", nameof(role));
            return identity.HasClaim(identity.RoleClaimType, role);
        }

        public static bool HasScope(this ClaimsIdentity identity, string scope)
        {
            if (string.IsNullOrEmpty(scope))
                throw new ArgumentException(@"Scope must be specified", nameof(scope));
            return identity.HasClaim("scope", scope);
        }
    }
}
