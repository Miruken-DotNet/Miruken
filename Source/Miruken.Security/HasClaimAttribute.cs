namespace Miruken.Security
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;

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

        protected override Task<bool> Allow(MethodBinding method,
            IPrincipal principal, IHandler composer)
        {
            var claims = ObtainClaims(principal, Type);
            if (claims.Any(claim => Values.Length == 0 ||
                Values.Contains(claim.Value)))
                return Task.FromResult(true);
            throw new UnauthorizedAccessException(
                $"The principal does not satisfy the required '{Type}' claim.");
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
