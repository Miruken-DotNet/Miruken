namespace Miruken.Mediator.Route
{
    using Callback;
    using Concurrency;

    public interface IRouter
    {
        bool CanRoute(Routed route, object message);

        Promise Route(Routed route, object message, IHandler composer);
    }
}
