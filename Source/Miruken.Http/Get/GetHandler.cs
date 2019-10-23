namespace Miruken.Http.Get
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Callback;

    public class GetHandler : ResourceHandler
    {
        [Handles]
        public async Task<GetResponse<TResource>> 
            Get<TGet, TResource>(GetRequest<TGet, TResource> get,
            HttpService http, IHandler composer)
        {
            var request  = GetRequest(get.Request, HttpMethod.Get)
                ?? new HttpRequestMessage(HttpMethod.Get, get.ResourceUri);
            var response = await http.SendRequest(get, request, http, composer,
                out var options).ConfigureAwait(false);
            var resource = await ExtractResource<TResource>(response, options);
            return new GetResponse<TResource>(resource);
        }
    }
}
