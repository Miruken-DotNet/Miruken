namespace Miruken.Http.Delete
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Callback;

    public class DeleteHandler : ResourceHandler
    {
        [Handles]
        public async Task<DeleteResponse<TResource>>
            Delete<TDelete, TResource>(DeleteRequest<TDelete, TResource> delete,
            HttpService http, IHandler composer)
        {
            var request  = GetRequest(delete.Request, HttpMethod.Delete)
                ?? new HttpRequestMessage(HttpMethod.Delete, delete.ResourceUri);
            var response = await http.SendRequest(delete, request, http, composer,
                out var options).ConfigureAwait(false);
            var resource = await ExtractResource<TResource>(response, options);
            return new DeleteResponse<TResource>(resource);
        }
    }
}
