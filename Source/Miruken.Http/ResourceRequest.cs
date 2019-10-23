namespace Miruken.Http
{
    using System;
    using System.Net.Http.Formatting;
    using Api;

    public abstract class ResourceRequest
    {
        public string             BaseAddress { get; set; }
        public string             ResourceUri { get; set; }
        public TimeSpan?          Timeout     { get; set; }
        public MediaTypeFormatter Formatter   { get; set; }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other?.GetType() != GetType())
                return false;

            return other is ResourceRequest otherRequest
                   && Equals(BaseAddress, otherRequest.BaseAddress)
                   && Equals(ResourceUri, otherRequest.ResourceUri)
                   && ReferenceEquals(Formatter, otherRequest.Formatter);
        }

        public override int GetHashCode()
        {
            return (BaseAddress?.GetHashCode() ?? 0) * 31 +
                   (ResourceUri?.GetHashCode() ?? 0);
        }
    }

    public abstract class ResourceRequest<TRequest, TResource>
        : ResourceRequest, IRequest<TResource>
    {
        protected ResourceRequest()
        {           
        }

        protected ResourceRequest(TRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            Request = request;
        }

        public TRequest Request { get; set; }

        public override bool Equals(object other)
        {
            if (!base.Equals(other)) return false;
            return other is ResourceRequest<TRequest, TResource> otherRequest 
                && Equals(Request, otherRequest.Request);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() * 31 + (Request?.GetHashCode() ?? 0);
        }
    }

    public abstract class ResourceResponse
    {
        public Uri ResourceUri { get; set; }
    }

    public abstract class ResourceResponse<TResource> : ResourceResponse
    {
        protected ResourceResponse()
        {
        }

        protected ResourceResponse(TResource resource)
        {
            Resource = resource;
        }

        public TResource Resource { get; set; }
    }
}