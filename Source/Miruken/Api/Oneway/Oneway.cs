namespace Miruken.Api.Oneway;

using System;

public class Oneway : IDecorator
{
    public Oneway()
    {
    }

    public Oneway(object request)
    {
        Request = request
                  ?? throw new ArgumentNullException(nameof(request));
    }

    public object Request { get; set; }

    object IDecorator.Decoratee => Request;

    public override bool Equals(object other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other is Oneway otherOneway
               && Equals(Request, otherOneway.Request);
    }

    public override int GetHashCode()
    {
        return Request?.GetHashCode() ?? 0;
    }
}

public static class OnewayExtensions
{
    public static Oneway Oneway<TResp>(this IRequest<TResp> request) => new(request);
}