namespace Miruken.Api.Oneway
{
    using System;
    using Api;

    public class Oneway<TResp> : IDecorator
    {
        public Oneway()
        {
        }

        public Oneway(IRequest<TResp> request)
        {
            Request = request
                   ?? throw new ArgumentNullException(nameof(request));
        }

        public IRequest<TResp> Request { get; set; }

        object IDecorator.Decoratee => Request;

        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return other is Oneway<TResp> otherOneway
                   && Equals(Request, otherOneway.Request);
        }

        public override int GetHashCode()
        {
            return Request?.GetHashCode() ?? 0;
        }
    }
}
