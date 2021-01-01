namespace Miruken.Http
{
    using System;
    using Api.Route;
    using Callback;
    using Concurrency;
    using Format;
    using Functional;

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
                .Then((response, _) => response.Match(
                    failure =>
                    {
                        var payload = failure.Payload;
                        throw payload switch
                        {
                            null => new Exception("An unexpected error has occurred."),
                            Exception exception => exception,
                            _ => new UnknownExceptionPayload(payload)
                        };
                    },
                    success => success.Payload));
        }

        private static string GetResourceUri(Routed routed,
            Command command, object message, IHandler composer)
        {
            var options = new HttpOptions();
            if (composer.Handle(options, true) && options.UriBuilder != null)
                return options.UriBuilder(routed, command, message, composer);
            return command.Many ? "Publish" : "Process";
        }
    }
}
