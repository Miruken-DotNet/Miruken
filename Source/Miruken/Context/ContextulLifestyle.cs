namespace Miruken.Context
{
    using System;
    using System.Collections.Concurrent;
    using Callback;
    using Callback.Policy;

    public class ContextualLifestyle<T> : Lifestyle<T>
    {
        protected override bool GetInstance(MemberBinding member,
            Next<T> next, IHandler composer, out T instance)
        {
            var context = composer.Resolve<IContext>();
            if (context == null)
            {
                instance = default;
                return false;
            }
            instance = _cache.GetOrAdd(context, ctx =>
            {
                var result = next().GetAwaiter().GetResult();
                if (result is IContextual contextual)
                {
                    contextual.Context = ctx;
                    contextual.ContextChanging += ThrowManagedContextException;
                    context.ContextEnded += _ =>
                    {
                        _cache.TryRemove(ctx, out var _);
                        contextual.ContextChanging -= ThrowManagedContextException;
                        contextual.Context = null;
                        (result as IDisposable)?.Dispose();
                    };
                }
                else
                    context.ContextEnded += _ => (result as IDisposable)?.Dispose();
                return result;
            });
            return true;
        }

        private static void ThrowManagedContextException(
            IContextual<IContext> contextual, IContext oldContext,
            ref IContext newContext)
        {
            throw new InvalidOperationException(
                "Managed instances cannot change the context");
        }

        private static readonly ConcurrentDictionary<IContext, T>
            _cache = new ConcurrentDictionary<IContext, T>();
    }

    public class ContextualAttribute : LifestyleAttribute
    {
        public ContextualAttribute()
            : base(typeof(ContextualLifestyle<>))
        {          
        }
    }
}
