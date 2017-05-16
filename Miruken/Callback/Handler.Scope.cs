namespace Miruken.Callback
{
    public class HandlerScope : HandlerDecorator
    {
        public HandlerScope(IHandler handler)
            : base(handler)
        {
        }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            var composition = callback as Composition;
            return !((composition?.Callback ?? callback) is IScopedCallback)
                   && base.HandleCallback(callback, greedy, composer);
        }
    }

    public static class HandlerScopeExtensions
    {
        public static IHandler Scope(this IHandler handler)
        {
            return handler != null ? new HandlerScope(handler) : null;
        }
    }
}
