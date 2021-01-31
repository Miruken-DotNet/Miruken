namespace Miruken.Http.Put
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Callback;

    public class PutHandler : ResourceHandler
    {
        [Handles]
        public async Task<PutResponse<TResource>>
            Put<TPut, TResource>(PutRequest<TPut, TResource> put,
            HttpService http, IHandler composer)
        {
            var request = GetRequest(put.Request, HttpMethod.Put)
                ?? new HttpRequestMessage(HttpMethod.Put, put.ResourceUri)
                   {
                       Content = GetContent(put.Request, put)
                   };
            var response    = await http.SendRequest(put, request, composer,
                    out var options).ConfigureAwait(false);
            var resource    = await ExtractResource<TResource>(response, options);
            var putResponse = new PutResponse<TResource>(resource);
            SetResponseUri(putResponse, response);
            return putResponse;
        }
    }
}
