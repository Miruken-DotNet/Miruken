namespace Miruken.Api.Once
{
    using System;
    using System.Threading.Tasks;
    using Api;
    using Callback;
    using Concurrency;

    public abstract class OnceHandler : Handler
    {
        [Handles]
        public Task Once(Once once, IHandler composer)
        {
            return Handle(once, composer, () => composer.Send(once.Request));
        }

        protected abstract Task Handle(Once once, IHandler composer, Func<Task> proceed);
    }

    public static class OnceExtensions
    {
        public static Once Once<TResp>(this IRequest<TResp> request)
        {
            return new Once(request);
        }
    }
}
