namespace Miruken.Context
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using Callback;
    using Callback.Policy.Bindings;

    public class ContextualLifestyle<T> : Lifestyle<T>
    {
        protected override bool IsCompatibleWithParent(Inquiry parent)
        {
            var parentDispatcher = parent?.Dispatcher;
            if (parentDispatcher == null) return true;
            return parentDispatcher.Attributes.OfType<LifestyleAttribute>()
                .All(lifestyle => lifestyle is ContextualAttribute);
        }

        protected override bool GetInstance(
            Inquiry inquiry, MemberBinding member,
            Next<T> next, IHandler composer,
            out T instance)
        {
            var context = composer.Resolve<Context>();
            if (context == null)
            {
                instance = default;
                return false;
            }

            instance = _cache.GetOrAdd(context, ctx =>
            {
                var result = next().Result;
                if (result is IContextual contextual)
                {
                    contextual.Context = ctx;
                    contextual.ContextChanging += ChangeContext;
                    context.ContextEnded += (c, r) =>
                    {
                        _cache.TryRemove(ctx, out _);
                        contextual.ContextChanging -= ChangeContext;
                        (result as IDisposable)?.Dispose();
                        contextual.Context = null;
                    };
                }
                else
                {
                    context.ContextEnded += (c, r)  =>
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

        private readonly ConcurrentDictionary<Context, T>
            _cache = new ConcurrentDictionary<Context, T>();
    }

    public class ContextualAttribute : LifestyleAttribute, IBindingConstraintProvider
    {
        public ContextualAttribute()
            : base(typeof(ContextualLifestyle<>))
        {          
        }

        public IBindingConstraint Constraint => Qualifier;

        private static readonly Qualifier Qualifier = Qualifier.Of<ContextualAttribute>();
    }
}
