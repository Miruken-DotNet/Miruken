namespace Miruken.Api.Route
{
    using Api;
    using Callback;
    using Concurrency;

    public class PassThroughRouter : Handler
    {
        public const string Scheme = "pass-through";

        [Handles]
        public Promise Route(Routed request, IHandler composer)
        {
            return request.Route == Scheme
                 ? composer.Send(request.Message)
                 : null;
        }
    }
}
