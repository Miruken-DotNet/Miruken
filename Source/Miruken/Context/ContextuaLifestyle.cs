namespace Miruken.Context
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy.Bindings;

    public class ContextualLifestyle<T> : Lifestyle<T>
    {
        protected override bool IsCompatibleWithParent(
            Inquiry parent, LifestyleAttribute attribute)
        {
            var parentDispatcher = parent?.Dispatcher;
            if (parentDispatcher == null) return true;
            return parentDispatcher.Attributes.OfType<LifestyleAttribute>()
                .All(lifestyle => lifestyle is ContextualAttribute c &&
                    ((attribute as ContextualAttribute)?.Rooted == true || !c.Rooted));
        }

        protected override async Task<T> GetInstance(
            Inquiry inquiry, MemberBinding member,
            Next<T> next, IHandler composer,
            LifestyleAttribute attribute)
        {
            var context = composer.Resolve<Context>();
            if (context == null)
                return await next(proceed: false);

            if ((attribute as ContextualAttribute)?.Rooted == true)
                context = context.Root;

            return await _cache.GetOrAdd(context, async ctx =>
            {
                var result = await next();
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
                ReferenceEquals(contextual, instance.Result))
            {
                _cache.TryRemove(oldContext, out _);
                (contextual as IDisposable)?.Dispose();
            }
        }

        private readonly ConcurrentDictionary<Context, Task<T>>
            _cache = new ConcurrentDictionary<Context, Task<T>>();
    }

    public class ContextualAttribute : LifestyleAttribute, IBindingConstraintProvider
    {
        public ContextualAttribute()
            : base(typeof(ContextualLifestyle<>))
        {          
        }

        public bool Rooted { get; set; }

        public IBindingConstraint Constraint => Qualifier;

        private static readonly Qualifier Qualifier = Qualifier.Of<ContextualAttribute>();
    }
}
