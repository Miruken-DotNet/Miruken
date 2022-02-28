namespace Miruken.Http.Get;

public class GetRequest<TGet, TResource>
    : ResourceRequest<TGet, GetResponse<TResource>>
{
    public GetRequest()
    {
    }

    public GetRequest(TGet request) : base(request)
    {
    }
}

public class GetResponse<TResource> : ResourceResponse<TResource>
{
    public GetResponse()
    {
    }

    public GetResponse(TResource resource)
        : base(resource)
    {
    }
}