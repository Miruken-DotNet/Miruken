namespace Miruken.Http
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Callback;

    public class HttpService
    {
        private readonly HttpClient _client;

        private static readonly HttpClient SharedHttpClient
            = new HttpClient(new HttpOptionsHandler
            {
                InnerHandler = new HttpClientHandler()
            });

        public static readonly HttpService Shared = new HttpService(SharedHttpClient);

        public HttpService(HttpClient client)
        {
            _client         = client;
            _client.Timeout = Timeout.InfiniteTimeSpan;
        }

        public Task<HttpResponseMessage> SendRequest(
            ResourceRequest request, HttpRequestMessage httpRequest,
            HttpService http, IHandler composer, out HttpOptions options)
        {
            options = new HttpOptions();
            composer.Handle(options, true);
            ConfigureHttpRequest(request, httpRequest, options, composer);
            return http._client.SendAsync(httpRequest);
        }

        private static void ConfigureHttpRequest(
            ResourceRequest request, HttpRequestMessage httpRequest,
            HttpOptions options, IHandler composer)
        {
            httpRequest.SetOptions(options);
            httpRequest.SetComposer(composer);
            httpRequest.SetTimeout(request.Timeout);

            var requestUri = httpRequest.RequestUri;
            if (requestUri == null)
            {
                var baseUrl = request.BaseAddress ?? options.BaseUrl;
                if (!string.IsNullOrEmpty(baseUrl))
                    requestUri = new Uri(baseUrl, UriKind.Absolute);
            }
            else if (!requestUri.IsAbsoluteUri)
            {
                var baseUrl = request.BaseAddress ?? options.BaseUrl;
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    if (!baseUrl.EndsWith(@"/")) baseUrl += @"/";
                    requestUri = new Uri(new Uri(baseUrl, UriKind.Absolute),
                        httpRequest.RequestUri);
                }
            }

            if (requestUri != null)
                httpRequest.RequestUri = requestUri;
        }
    }
}
