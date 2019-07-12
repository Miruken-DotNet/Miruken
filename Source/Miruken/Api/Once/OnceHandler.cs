namespace Miruken.Api.Once
{
    using System.Threading.Tasks;
    using Callback;
    using Map;

    public class OnceHandler : Handler
    {
        [Handles]
        public Task Once(Once once, IHandler composer)
        {
            var strategy = composer.Map<IOnceStrategy>(once.Request);
            return strategy.Complete(once, composer);
        }
    }
}
