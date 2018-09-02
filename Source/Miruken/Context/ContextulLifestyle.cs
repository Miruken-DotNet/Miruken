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
            var context = composer.Resolve<Context>();
            if (context == null)
            {
                instance = default;
                return false;
            }
            instance = _cache.GetOrAdd(context, ctx =>
            {
                var result = next().GetAwaiter().GetResult();
                if (result is Contextual contextual)
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

        private void ThrowManagedContextException(
            IContextual contextual, Context oldContext,
            ref Context newContext)
        {
            if (oldContext == newContext) return;
            if (newContext != null)
            {
                throw new InvalidOperationException(
                    "Managed instances cannot change the context");
            }
            if (_cache.TryGetValue(oldContext, out var instance) &&
                ReferenceEquals(contextual, instance))
            {
                _cache.TryRemove(oldContext, out _);
                (contextual as IDisposable)?.Dispose();
            }
        }

        private readonly ConcurrentDictionary<Context, T>
            _cache = new ConcurrentDictionary<Context, T>();
    }

    public class ContextualAttribute : LifestyleAttribute
    {
        public ContextualAttribute()
            : base(typeof(ContextualLifestyle<>))
        {          
        }
    }
}
