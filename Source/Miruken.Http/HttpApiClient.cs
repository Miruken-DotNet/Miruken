namespace Miruken.Http
{
    using Api;
    using Api.Route;
    using Callback;
    using Callback.Policy;
    using Concurrency;
    using Delete;
    using Get;
    using Patch;
    using Post;
    using Put;
    using Validate;

    public static class HttpApiClient
    {
        public static Handler Handler { get; } =
            new CompositeHandler(HttpService.Shared,
                new HttpRouter(),        new PostHandler(),
                new GetHandler(),        new PutHandler(),
                new DeleteHandler(),     new PatchHandler(),
                new ValidationMapping(), new ErrorMapping());

        public static Promise Send(
            object request, string route, string tag = null) =>
            Handler.Send(request.RouteTo(route, tag));

        public static Promise<TResp> Send<TResp>(
            object request, string route, string tag = null) =>
            Handler.Send<TResp>(request.RouteTo(route, tag));

        public static Promise<TResp> Send<TResp>(
            IRequest<TResp> request, string route, string tag = null) =>
            Handler.Send(request.RouteTo(route, tag));

        public static Promise Publish(
            object notification, string route, string tag = null) =>
            Handler.Publish(notification.RouteTo(route, tag));

        public static void Register(IHandlerDescriptorFactory factory)
        {
            factory.RegisterDescriptor<Provider>();
            factory.RegisterDescriptor<HttpRouter>();
            factory.RegisterDescriptor<GetHandler>();
            factory.RegisterDescriptor<PostHandler>();
            factory.RegisterDescriptor<PutHandler>();
            factory.RegisterDescriptor<PatchHandler>();
            factory.RegisterDescriptor<DeleteHandler>();
            factory.RegisterDescriptor<ValidationMapping>();
            factory.RegisterDescriptor<ErrorMapping>();
        }

        static HttpApiClient()
        {
            Register(HandlerDescriptorFactory.Current);
        }
    }
}
