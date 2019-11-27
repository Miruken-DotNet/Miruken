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
            return parentBinding.Filters.OfType<LifestyleAttribute>()
                .All(lifestyle => lifestyle is ContextualAttribute c &&
                    ((attribute as ContextualAttribute)?.Rooted == true || !c.Rooted));
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
                        (instance as IDisposable)?.Dispose();
                        contextual.Context = null;
                    };
                }
                else
                {
                    context.ContextEnded += (c, r)  =>
                    {
                        _cache.TryRemove(ctx, out _);
                        (instance as IDisposable)?.Dispose();
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

        public bool Rooted { get; set; }

        public IBindingConstraint Constraint => Qualifier;

        private static readonly Qualifier Qualifier = Qualifier.Of<ContextualAttribute>();
    }
}
