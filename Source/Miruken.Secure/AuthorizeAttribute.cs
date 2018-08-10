namespace Miruken.Secure
{
    using System;
    using System.Linq;
    using Callback;
    using Callback.Policy;

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
            Type filterType, MethodBinding binding)
        {
            return !binding.Dispatcher.Attributes
                .OfType<AllowAttribute>().Any();
        }
    }
}
