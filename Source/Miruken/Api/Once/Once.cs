namespace Miruken.Api.Once;

using System;

public class Once : IDecorator
{
    public Once()
    {
        RequestId = Guid.NewGuid();
    }

    public Once(object request) : this()
    {
        Request = request
                ?? throw new ArgumentNullException(nameof(request));
    }

    public Guid RequestId { get; set; }

    public object Request { get; set; }

    object IDecorator.Decoratee => Request;

    public override bool Equals(object other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other is Once otherOnce && Equals(Request, otherOnce.Request);
    }

    public override int GetHashCode()
    {
        return Request?.GetHashCode() ?? 0;
    }
}

public static class OnceExtensions
{
    public static Once Once(this object request, Guid? requestId = null)
    {
        var once = new Once(request);
        if (requestId != null)
            once.RequestId = requestId.Value;
        return once;
    }
}