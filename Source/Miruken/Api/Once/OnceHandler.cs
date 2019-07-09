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
            return IsHandled(once, composer)
                .Then((handled, _) => handled ? Promise.Empty
                    : composer.Send(once.Request)
                        .Then((r, __) => Handled(once, composer))
                    );
        }

        protected abstract Promise<bool> IsHandled(Once once, IHandler composer);

        protected abstract Promise Handled(Once once, IHandler composer);
    }

    public static class OnceExtensions
    {
        public static Once Once<TResp>(this IRequest<TResp> request)
        {
            return new Once(request);
        }
    }
}
