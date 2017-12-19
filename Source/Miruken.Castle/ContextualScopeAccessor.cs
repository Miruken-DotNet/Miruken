namespace Miruken.Castle
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Callback;
    using Context;
    using global::Castle.Core;
    using global::Castle.Core.Internal;
    using global::Castle.MicroKernel;
    using global::Castle.MicroKernel.Context;
    using global::Castle.MicroKernel.Lifestyle.Scoped;
    using IHandler = Callback.IHandler;

    public class ContextualScopeAccessor : IScopeAccessor
    {
        private readonly ConcurrentDictionary<IContext, ILifetimeScope>
            _cache = new ConcurrentDictionary<IContext, ILifetimeScope>();

        public ILifetimeScope GetScope(CreationContext creationContext)
        {
            if (!creationContext.RequestedType.Is<IContextual>())
                return null;
            var extra = creationContext.AdditionalArguments;
            var composer = extra[WindsorHandler.ComposerKey] as IHandler;
            var context = composer?.Resolve<IContext>();
            return context == null ? null : _cache.GetOrAdd(context, CreateContextScope);
        }

        private ILifetimeScope CreateContextScope(IContext context)
        {
            var scope = new ContextLifetimeScope(context);
            context.ContextEnded += ctx =>
            {
                if (_cache.TryRemove(ctx, out var lifetimeScope))
                    lifetimeScope.Dispose();
            };
            return scope;
        }

        public void Dispose()
        {
            foreach (var scope in _cache)
                scope.Value.Dispose();
            _cache.Clear();
        }

        private class ContextLifetimeScope : ILifetimeScope
        {
            private readonly IContext _context;
            private readonly Lock _lock = Lock.Create();
            private IDictionary<object, Burden> _cache;

            public ContextLifetimeScope(IContext context)
            {
                _context = context;
                _cache = new Dictionary<object, Burden>();
            }

            public void Dispose()
            {
                using (var token = _lock.ForReadingUpgradeable())
                {
                    if (_cache == null) return;
                    token.Upgrade();
                    var localCache = Interlocked.Exchange(ref _cache, null);
                    var burdens    = localCache?.Values.Reverse();
                    if (burdens != null)
                    {
                        foreach (var burden in burdens)
                            burden.Release();
                    }
                }
            }

            public Burden GetCachedInstance(
                ComponentModel model, ScopedInstanceActivationCallback createInstance)
            {
                AssertNotDisposed();
                using (var token = _lock.ForReadingUpgradeable())
                {
                    if (_cache.TryGetValue(model, out var burden)) return burden;
                    token.Upgrade();
                    burden = createInstance(OnCreated);
                    _cache[model] = burden;
                    return burden;
                }
            }

            private void OnCreated(Burden burden)
            {
                var contextual = (IContextual) burden.Instance;
                var existingContext = contextual.Context;
                if (existingContext == null)
                    contextual.Context = _context;
                else if (existingContext != _context)
                    throw new InvalidOperationException(
                        $"Component {contextual.GetType().FullName} is already bound to a context");

                void Changing(IContextual<IContext> _, IContext oldContext, ref IContext newContext)
                {
                    if (newContext != null)
                        throw new InvalidOperationException(
                            "Container managed instances cannot change context");
                }

                void Changed(IContextual<IContext> _, IContext oldContext, IContext newContext)
                {
                    _cache.Remove(burden.Handler.ComponentModel);
                    burden.Release();
                }

                burden.Releasing += b =>
                {
                    contextual.ContextChanging -= Changing;
                    contextual.ContextChanged -= Changed;
                };
                burden.Released += b => contextual.Context = null;

                contextual.ContextChanging += Changing;
                contextual.ContextChanged += Changed;
            }

            private void AssertNotDisposed()
            {
                if (_cache == null)
                    throw new ObjectDisposedException(
                        "Scope cache was already disposed. This is most likely a bug in the calling code.");
            }
        }
    }
}
