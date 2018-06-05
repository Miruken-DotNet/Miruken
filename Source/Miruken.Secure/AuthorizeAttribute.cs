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

        public bool AllowAnonymousUser { get; set; }

        protected override bool AllowFilterType(
            Type filterType, MethodBinding binding)
        {
            return !binding.Dispatcher.Attributes
                .OfType<AllowAttribute>().Any();
        }
    }
}
