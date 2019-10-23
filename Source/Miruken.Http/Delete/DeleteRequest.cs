namespace Miruken.Http.Delete
{
    public class DeleteRequest<TDelete, TResource>
        : ResourceRequest<TDelete, DeleteResponse<TResource>>
    {
        public DeleteRequest()
        {
        }

        public DeleteRequest(TDelete request) : base(request)
        {
        }
    }

    public class DeleteResponse<TResource>
    {
        public DeleteResponse()
        {
        }

        public DeleteResponse(TResource resource)
        {
            Resource = resource;
        }

        public string    ResourceUri { get; set; }
        public TResource Resource    { get; set; }
    }
}
