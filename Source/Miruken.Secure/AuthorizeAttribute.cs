namespace Miruken.Secure;

using System;
using System.Linq;
using Callback;
using Callback.Policy.Bindings;

public class AuthorizeAttribute : FilterAttribute
{
    public AuthorizeAttribute()
        : base(typeof(AuthorizeFilter<,>))
    {
        Required = true;
    }

    public bool AllowAnonymous { get; init; }

    public bool NoAccessPolicy { get; init; }

    protected override bool AcceptFilterType(Type filterType, MemberBinding binding) =>
        !binding.Dispatcher.Attributes.OfType<AllowAttribute>().Any();
}