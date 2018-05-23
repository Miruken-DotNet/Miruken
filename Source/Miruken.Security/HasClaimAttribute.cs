namespace Miruken.Security
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Principal;
    using Callback;

    public class HasClaimAttribute : ClaimsAccessDecision
    {
        public HasClaimAttribute(string type, params string[] values)
        {
            Type     = type   ?? throw new ArgumentNullException(nameof(type));
            Values   = values ?? throw new ArgumentNullException(nameof(values));
            Required = true;
        }

        public string   Type   { get; }
        public string[] Values { get; }

        protected override bool Allow(IPrincipal principal, IHandler composer)
        {
            var claims = ObtainClaims(principal, Type);
            if (claims.Any(claim => Values.Length == 0 ||
                Values.Contains(claim.Value))) return true;
            throw new UnauthorizedAccessException();
        }
    }

    public class HasRoleAttribute : HasClaimAttribute
    {
        public HasRoleAttribute(params string[] roles)
            : base(ClaimTypes.Role, roles)
        {
        }
    }
}
