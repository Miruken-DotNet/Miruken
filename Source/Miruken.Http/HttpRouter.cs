namespace Miruken.Http
{
    using System;
    using Api.Route;
    using Callback;
    using Concurrency;
    using Format;
    using Functional;
    using Map;

    [Routes("http", "https")]
    public class HttpRouter : Handler
    {
        [Provides, Singleton]
        public HttpRouter()
        {
        }

        [Handles]
        public Promise Route(Routed routed,
            Command command, IHandler composer)
        {
            var message   = routed.Message;
            var uri       = GetResourceUri(routed, command, message, composer);
            var formatter = new HttpRouteMediaTypeFormatter(composer);

            return composer
                .EnableFilters()
                .Formatters(formatter)
                .HttpPost<Message, Try<Message, Message>>(
                    new Message(message), post =>
                    {
                        post.BaseAddress = routed.Route;
                        post.ResourceUri = uri;
                        post.Formatter   = formatter;
                    }).ToPromise()
                .Then((response, s) => response.Match(
                    failure =>
                    {
                        var payload = failure.Payload;
                        if (payload == null)
                            throw new Exception("An unexpected error has occurred");
                        throw composer.Map<Exception>(failure.Payload, typeof(Exception));
                    },
                    success => success.Payload));
        }

        private static string GetResourceUri(Routed route,
            Command command, object message, IHandler composer)
        {
            var options = new HttpOptions();
            if (composer.Handle(options, true) && options.UriBuilder != null)
                return options.UriBuilder(route, command, message, composer);
            return command.Many ? "Publish" : "Process";
        }
    }
}
