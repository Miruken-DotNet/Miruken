namespace Miruken.Http
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Api.Route;
    using Callback;

    public delegate string UriBuilderDelegate(Routed route,
        Command command, object message, IHandler composer);

    public delegate Task<HttpResponseMessage> HttpRequestPipeline(
        HttpRequestMessage request, CancellationToken cancellationToken,
        IHandler composer, Func<Task<HttpResponseMessage>> next);

    public class HttpOptions : Options<HttpOptions>
    {
        public string                       BaseUrl    { get; set; }
        public TimeSpan?                    Timeout    { get; set; }
        public UriBuilderDelegate           UriBuilder { get; set; }
        public HttpRequestPipeline          Pipeline   { get; set; }
        public MediaTypeFormatterCollection Formatters { get; set; }

        public override void MergeInto(HttpOptions other)
        {
            if (BaseUrl != null && other.BaseUrl == null)
                other.BaseUrl = BaseUrl;

            if (Timeout.HasValue && !other.Timeout.HasValue)
                other.Timeout = Timeout;

            if (UriBuilder != null && other.UriBuilder == null)
                other.UriBuilder = UriBuilder;

            if (Pipeline != null)
                other.Pipeline += Pipeline;

            if (Formatters != null)
            {
                var otherFormatters = other.Formatters;
                if (otherFormatters == null)
                    other.Formatters = new MediaTypeFormatterCollection(Formatters);
                else
                    otherFormatters.AddRange(Formatters);
            }
        }
    }

    public static class HttpOptionsExtensions
    {
        public static IHandler BaseUrl(
            this IHandler handler, string baseUrl)
        {
            return new HttpOptions { BaseUrl = baseUrl }.Decorate(handler);
        }

        public static IHandler BaseUrl(
            this IHandler handler, Func<IHandler, string> baseUrl)
        {
            if (baseUrl == null)
                throw new ArgumentNullException(nameof(baseUrl));
            return handler.BaseUrl(baseUrl(handler));
        }

        public static IHandler Timeout(
            this IHandler handler, TimeSpan timeout)
        {
            return new HttpOptions { Timeout = timeout }.Decorate(handler);
        }

        public static IHandler UriBuilder(
            this IHandler handler, UriBuilderDelegate uriBuilder)
        {
            return new HttpOptions { UriBuilder = uriBuilder }.Decorate(handler);
        }

        public static IHandler Pipeline(
            this IHandler handler, HttpRequestPipeline pipeline)
        {
            return new HttpOptions { Pipeline = pipeline }.Decorate(handler);
        }

        public static IHandler UseRequestPath(IHandler handler)
        {
            return UriBuilder(handler, GetRequestPath);
        }

        public static IHandler Formatters(
            this IHandler handler, MediaTypeFormatterCollection formatters)
        {
            return new HttpOptions { Formatters = formatters }.Decorate(handler);
        }

        public static IHandler Formatters(
            this IHandler handler, params MediaTypeFormatter[] formatters)
        {
            return new HttpOptions
            {
                Formatters = new MediaTypeFormatterCollection(formatters)
            }.Decorate(handler);
        }

        private static string GetRequestPath(
            Routed route, Command command, object message, IHandler composer)
        {
            if (!string.IsNullOrEmpty(route.Tag))
                return $"tag/{GetApplicationName()}/{route.Tag}";
            var prefix = command.Many ? "Publish" : "Process";
            var path   = GetRequestPath(message);
            return path != null ? $"{prefix}/{path}" : prefix;
        }

        public static string GetRequestPath(Type requestType)
        {
            if (requestType.IsGenericTypeDefinition)
                return null;
            var path = requestType.ToString();
            if (typeof(IDecorator).IsAssignableFrom(requestType))
            {
                var name  = requestType.Name;
                var index = name.IndexOf('`');
                path = index < 0 ? name : name.Substring(0, index);
            }
            var parts = path.Split('.');
            return string.Join("/", parts.Select(part =>
                char.ToLower(part[0]) + part.Substring(1))
                .ToArray());
        }

        public static string GetRequestPath(object request)
        {
            var decorators = "";
            while (request is IDecorator)
            {
                var decorator = GetRequestPath(request.GetType());
                decorators = $"{decorators}/{decorator}";
                request    = ((IDecorator)request).Decoratee;
            }
            var basePath = GetRequestPath(request.GetType());
            return basePath == null ? null : $"{basePath}{decorators}";
        }

        private static string GetApplicationName()
        {
            var entry = Assembly.GetEntryAssembly();
            return entry != null
                 ? entry.GetName().Name
                 : AppDomain.CurrentDomain.FriendlyName;
        }
    }
}
