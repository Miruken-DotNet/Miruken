namespace Miruken.Http.Patch;

using System.Net.Http;
using System.Threading.Tasks;
using Callback;

public class PatchHandler : ResourceHandler
{
    [Handles]
    public async Task<PatchResponse<TResource>>
        Patch<TPatch, TResource>(PatchRequest<TPatch, TResource> patch,
            HttpService http, IHandler composer)
    {
        var request = GetRequest(patch.Request, HttpExtensions.PatchMethod)
                      ?? new HttpRequestMessage(HttpExtensions.PatchMethod, patch.ResourceUri)
                      {
                          Content = GetContent(patch.Request, patch)
                      };
        var response      = await http.SendRequest(patch, request, composer,
            out var options).ConfigureAwait(false);
        var resource      = await ExtractResource<TResource>(response, options);
        var patchResponse = new PatchResponse<TResource>(resource);
        SetResponseUri(patchResponse, response);
        return patchResponse;
    }
}