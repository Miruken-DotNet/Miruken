#if NETFULL
namespace Miruken.Http
{
    using Callback;

    public class HttpServiceProvider : Handler
    {
        [Provides]
        public HttpService GetShared() => HttpService.Shared;
    }
}
#endif