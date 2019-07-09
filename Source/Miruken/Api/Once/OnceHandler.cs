namespace Miruken.Api.Once
{
    using System;
    using Api;
    using Callback;
    using Concurrency;

    public abstract class OnceHandler : Handler
    {
        [Handles]
        public Promise Once(Once once, IHandler composer)
        {
            return Handle(once, composer, () => composer.Send(once.Request));
        }

        protected abstract Promise Handle(Once once, IHandler composer, Func<Promise> proceed);
    }

    public static class OnceExtensions
    {
        public static Once Once<TResp>(this IRequest<TResp> request)
        {
            return new Once(request);
        }
    }
}
