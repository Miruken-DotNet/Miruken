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
            var parentBinding = parent?.Binding;
            if (parentBinding == null) return true;
            var rooted = (attribute as ContextualAttribute)?.Rooted;
            return parentBinding.Filters.OfType<LifestyleAttribute>()
                .All(lifestyle => lifestyle is ContextualAttribute c &&
                    (rooted == true || !c.Rooted));
        }

        protected override Task<T> GetInstance(
            Inquiry inquiry, MemberBinding member,
            Next<T> next, IHandler composer,
            LifestyleAttribute attribute)
        {
            var context = composer.Resolve<Context>();
            if (context == null)
                return next(proceed: false);

            if ((attribute as ContextualAttribute)?.Rooted == true)
                context = context.Root;

            return Task.FromResult(_cache.GetOrAdd(context, ctx =>
            {
                var instance = next().GetAwaiter().GetResult();
                if (instance is IContextual contextual)
                {
                    contextual.Context = ctx;
                    contextual.ContextChanging += ChangeContext;
                    context.ContextEnded += (c, r) =>
                    {
                        _cache.TryRemove(ctx, out _);
                        contextual.ContextChanging -= ChangeContext;
                        TryDispose(instance);
                        contextual.Context = null;
                    };
                }
                else
                {
                    context.ContextEnded += (c, r)  =>
                    {
                        _cache.TryRemove(ctx, out _);
                        TryDispose(instance);
                    };
                }
                return instance;
            }));
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

            if (!(_cache.TryGetValue(oldContext, out var instance) &&
                ReferenceEquals(contextual, instance)))
                return;
            
            _cache.TryRemove(oldContext, out _);
            TryDispose(contextual);
        }

        private static void TryDispose(object instance)
        {
            switch (instance)
            {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                case IAsyncDisposable asyncDisposable:
                    var task = asyncDisposable.DisposeAsync();
                    if (!task.IsCompleted)
                        task.AsTask().GetAwaiter().GetResult();
                    break;
            }
        }
        
        private readonly ConcurrentDictionary<Context, T>
            _cache = new();
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
