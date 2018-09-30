namespace Miruken.Secure
{
    using System;
    using System.Linq;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;

    public class AuthorizeAttribute : FilterAttribute
    {
        public AuthorizeAttribute()
            : base(typeof(AuthorizeFilter<,>))
        {
            Required = true;
        }

        public bool AllowAnonymous { get; set; }

        public bool NoAccessPolicy { get; set; }

        protected override bool AcceptFilterType(
            Type filterType, MemberBinding binding)
        {
            return !binding.Dispatcher.Attributes
                .OfType<AllowAttribute>().Any();
        }
    }
}
