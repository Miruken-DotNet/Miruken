namespace Miruken.Callback
{
    public class HandlerBounds : HandlerDecorator
    {
        private readonly object _boundary;

        public HandlerBounds(IHandler handler, object boundary = null)
            : base(handler)
        {
            _boundary = boundary;
        }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            var composition = callback as Composition;
            var bounded     = (composition?.Callback ?? callback) as IBoundCallback;
            return (bounded == null || bounded.Boundary != _boundary) &&
                base.HandleCallback(callback, greedy, composer);
        }
    }

    public static class HandlerScopeExtensions
    {
        public static IHandler Stop(this IHandler handler, object boundary = null)
        {
            return handler != null ? new HandlerBounds(handler, boundary) : null;
        }
    }
}
