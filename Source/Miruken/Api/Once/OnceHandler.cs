namespace Miruken.Api.Once
{
    using Api;
    using Callback;
    using Concurrency;

    public abstract class OnceHandler : Handler
    {
        [Handles]
        public Promise Once(Once once, IHandler composer)
        {
            return Handle(once, composer);
        }

        protected abstract Promise Handle(Once once, IHandler composer);
    }

    public static class OnceExtensions
    {
        public static Once Once<TResp>(this IRequest<TResp> request)
        {
            return new Once(request);
        }
    }
}
