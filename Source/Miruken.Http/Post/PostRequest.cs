namespace Miruken.Http.Post
{
    public class PostRequest<TPost, TResource>
          : ResourceRequest<TPost, PostResponse<TResource>>
    {
        public PostRequest()
        {
        }

        public PostRequest(TPost request) : base(request)
        {
        }
    }

    public class PostResponse<TResource> : ResourceResponse<TResource>
    {
        public PostResponse()
        {          
        }

        public PostResponse(TResource resource)
            : base(resource)
        {
        }
    }
}
