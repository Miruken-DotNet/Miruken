namespace Miruken.Callback
{
    public class HandlerBounds : HandlerDecorator
    {
        private readonly object _bounds;

        public HandlerBounds(IHandler handler, object bounds = null)
            : base(handler)
        {
            _bounds = bounds;
        }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            var composition = callback as Composition;
            var bounded     = (composition?.Callback ?? callback) as IBoundCallback;
            return (bounded == null || bounded.Bounds != _bounds) &&
                base.HandleCallback(callback, ref greedy, composer);
        }
    }

    public static class HandlerScopeExtensions
    {
        public static IHandler Stop(this IHandler handler, object bounds = null)
        {
            return handler != null ? new HandlerBounds(handler, bounds) : null;
        }
    }
}
