namespace Miruken.Api.Oneway
{
    using Api;
    using Callback;
    using Concurrency;

    public class OnewayHandler : Handler
    {
        [Provides, Singleton]
        public OnewayHandler()
        {           
        }

        [Handles]
        public Promise Oneway<TResp>(Oneway<TResp> oneway, IHandler composer)
        {
            return composer.Send(oneway.Request);
        }
    }

    public static class OnewayExtensions
    {
        public static Oneway<TResp> Oneway<TResp>(this IRequest<TResp> request)
        {
            return new Oneway<TResp>(request);
        }
    }
}
