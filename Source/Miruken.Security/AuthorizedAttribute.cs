namespace Miruken.Security
{
    using Callback;

    public class AuthorizedAttribute : FilterAttribute
    {
        public AuthorizedAttribute()
            : base(typeof(AuthorizeFilter<,>))
        {           
        }

        public AuthorizedAttribute(string policy) : this()
        {
            Policy = policy;
        }

        public string Policy { get; }
    }
}
