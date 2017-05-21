namespace Miruken.Callback
{
    public class HandlerScope : HandlerDecorator
    {
        private readonly object _scope;

        public HandlerScope(IHandler handler, object scope = null)
            : base(handler)
        {
            _scope = scope;
        }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            var composition = callback as Composition;
            var scoped = (composition?.Callback ?? callback) as IScopedCallback;
            return (scoped == null || scoped.Scope != _scope) &&
                base.HandleCallback(callback, greedy, composer);
        }
    }

    public static class HandlerScopeExtensions
    {
        public static IHandler Scope(this IHandler handler, object scope = null)
        {
            return handler != null ? new HandlerScope(handler, scope) : null;
        }
    }
}
