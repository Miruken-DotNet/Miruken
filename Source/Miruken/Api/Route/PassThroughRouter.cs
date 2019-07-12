namespace Miruken.Api.Route
{
    using Api;
    using Callback;
    using Concurrency;

    [Unmanaged]
    public class PassThroughRouter : Handler
    {
        public const string Scheme = "pass-through";

        [Handles, SkipFilters]
        public Promise Route(Routed request, IHandler composer)
        {
            return request.Route == Scheme
                 ? composer.Send(request.Message)
                 : null;
        }
    }
}
