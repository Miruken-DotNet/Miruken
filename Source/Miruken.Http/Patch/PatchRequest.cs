namespace Miruken.Http.Patch;

public class PatchRequest<TPatch, TResource>
    : ResourceRequest<TPatch, PatchResponse<TResource>>
{
    public PatchRequest()
    {
    }

    public PatchRequest(TPatch request) : base(request)
    {
    }
}

public class PatchResponse<TResource> : ResourceResponse<TResource>
{
    public PatchResponse()
    {          
    }

    public PatchResponse(TResource resource)
        : base(resource)
    {
    }
}