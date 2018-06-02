namespace Miruken.Security
{
    using Callback;

    public class AuthorizationOptions : Options<AuthorizationOptions>
    {
        public bool? RequireAuthenticatedUser { get; set; }
        public bool? RequirePolicy            { get; set; }

        public override void MergeInto(AuthorizationOptions other)
        {
            if (RequireAuthenticatedUser.HasValue &&
                !other.RequireAuthenticatedUser.HasValue)
                other.RequireAuthenticatedUser = RequireAuthenticatedUser;

            if (RequirePolicy.HasValue && !other.RequirePolicy.HasValue)
                other.RequirePolicy = RequirePolicy;
        }
    }
}
