namespace Miruken.Http.Put
{
    public class PutRequest<TPut, TResource>
         : ResourceRequest<TPut, PutResponse<TResource>>
    {
        public PutRequest()
        {
        }

        public PutRequest(TPut request) : base(request)
        {
        }
    }

    public class PutResponse<TResource> : ResourceResponse<TResource>
    {
        public PutResponse()
        {
        }

        public PutResponse(TResource resource)
            : base(resource)
        {
        }
    }
}
