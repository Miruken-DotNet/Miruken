namespace Miruken.Secure
{
    using System;
    using System.Security.Claims;
    using System.Security.Principal;
    using Callback;
    using Callback.Policy;
    using Infrastructure;

    public class ClaimAttribute : Attribute, IArgumentResolver
    {
        public ClaimAttribute(string claimType)
        {
            if (string.IsNullOrEmpty(claimType))
                throw new ArgumentException("ClaimType cannot be empty");
            ClaimType = claimType;
        }

        public string ClaimType { get; }

        public bool IsOptional => false;

        public void ValidateArgument(Argument argument)
        {
            if (!argument.ParameterType.IsSimpleType())
                throw new NotSupportedException(
                    "Claim parameters must be simple types");
        }

        public object ResolveArgument(Inquiry parent,
            Argument argument, IHandler handler, IHandler composer)
        {
            var principal = composer.Resolve<IPrincipal>();
            if (!(principal?.Identity is ClaimsIdentity identity))
                throw new RejectedException();
            var claim = identity.FindFirst(ClaimType);
            if (claim == null)
                throw new RejectedException($"Missing claim '{ClaimType}'");
            try
            {
                return RuntimeHelper.ChangeType(claim.Value, argument.ParameterType);
            }
            catch (Exception ex)
            {
                throw new RejectedException(
                    $"Unable to extract claim '{ClaimType}'", ex);
            }
        }
    }
}
