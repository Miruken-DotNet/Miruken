namespace Miruken.Context
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using Callback;
    using Callback.Policy.Bindings;

    public class ContextualLifestyle<T> : Lifestyle<T>
    {
        protected override bool GetInstance(
            Inquiry inquiry, MemberBinding member,
            Next<T> next, IHandler composer,
            out T instance)
        {
            if (!CheckCompatibleParent(inquiry.Parent))
            {
                instance = default;
                return false;
            }

            var context = composer.Resolve<Context>();
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
                    ctx.RemoveHandlers(result);
                    contextual.ContextChanging += ChangeContext;
                    context.ContextEnded += c =>
                    {
                        _cache.TryRemove(ctx, out _);
                        contextual.ContextChanging -= ChangeContext;
                        contextual.Context = null;
                        (result as IDisposable)?.Dispose();
                    };
                }
                else
                {
                    context.ContextEnded += c =>
                    {
                        _cache.TryRemove(ctx, out _);
                        (result as IDisposable)?.Dispose();
                    };
                }
                return result;
            });

            return true;
        }

        private void ChangeContext(IContextual contextual,
            Context oldContext, ref Context newContext)
        {
            if (oldContext == newContext) return;
            if (newContext != null)
            {
                throw new InvalidOperationException(
                    "Managed instances cannot change context");
            }
            if (_cache.TryGetValue(oldContext, out var instance) &&
                ReferenceEquals(contextual, instance))
            {
                _cache.TryRemove(oldContext, out _);
                (contextual as IDisposable)?.Dispose();
            }
        }

        private static bool CheckCompatibleParent(Inquiry parent)
        {
            var parentDispatcher = parent?.Dispatcher;
            if (parentDispatcher == null) return true;
            return parentDispatcher.Attributes.OfType<LifestyleAttribute>()
                .All(lifestyle => lifestyle is ContextualAttribute);
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
