namespace Miruken.Api.Route
{
    using Api;

    public static class RouteExtensions
    {
        public static Routed RouteTo(
            this object request, string route, string tag = null)
        {
            return new Routed(request)
            {
                Route = route,
                Tag   = tag
            };
        }

        public static RoutedRequest<TResponse> RouteTo<TResponse>(
            this IRequest<TResponse> request, string route, string tag = null)
        {
            return new RoutedRequest<TResponse>(request)
            {
                Route = route,
                Tag   = tag
            };
        }
    }
}
