namespace Miruken.Mediator
{
    using Callback;
    using Concurrency;

    public class OnewayHandler : Handler
    {
        [Mediates]
        public Promise Oneway<TResponse>(
            Oneway<TResponse> request, IHandler composer)
        {
            return composer.Send(request.Request);
        }
    }

    public static class OnewayExtensions
    {
        public static Oneway Oneway<TResp>(this IRequest<TResp> request)
        {
            return new Oneway<TResp>(request);
        }
    }
}
