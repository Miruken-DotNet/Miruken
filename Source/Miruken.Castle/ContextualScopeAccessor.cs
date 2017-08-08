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
            Cache = new ConcurrentDictionary<IContext, ILifetimeScope>();

        public ILifetimeScope GetScope(CreationContext creationContext)
        {
            if (!creationContext.RequestedType.Is<IContextual>())
                return null;
            var extra = creationContext.AdditionalArguments;
            var composer = extra[WindsorHandler.ComposerKey] as IHandler;
            var context = composer?.Resolve<IContext>();
            return context == null ? null : Cache.GetOrAdd(context, CreateContextScope);
        }

        private ILifetimeScope CreateContextScope(IContext context)
        {
            var scope = new ContextLifetimeScope(context);
            context.ContextEnded += ctx =>
            {
                ILifetimeScope lifetimeScope;
                if (Cache.TryRemove(ctx, out lifetimeScope))
                    lifetimeScope.Dispose();
            };
            return scope;
        }

        public void Dispose()
        {
            foreach (var scope in Cache)
                scope.Value.Dispose();
            Cache.Clear();
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
                    localCache?.Values.Reverse().ForEach(b => b.Release());
                    _cache = null;
                }
            }

            public Burden GetCachedInstance(ComponentModel model, ScopedInstanceActivationCallback createInstance)
            {
                AssertNotDisposed();
                using (var token = _lock.ForReadingUpgradeable())
                {
                    Burden burden;
                    if (!_cache.TryGetValue(model, out burden))
                    {
                        token.Upgrade();
                        burden = createInstance(OnCreated);
                        _cache[model] = burden;
                    }
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

                ContextChangingDelegate<IContext> changing =
                    (IContextual<IContext> _, IContext oldContext, ref IContext newContext) =>
                    {
                        if (newContext != null)
                            throw new InvalidOperationException(
                                "Container managed instances cannot change context");
                    };

                ContextChangedDelegate<IContext> changed = (_, oldContext, newContext) =>
                {
                    _cache.Remove(burden.Handler.ComponentModel);
                    burden.Release();
                };

                burden.Releasing += b =>
                {
                    contextual.ContextChanging -= changing;
                    contextual.ContextChanged -= changed;
                };
                burden.Released += b => contextual.Context = null;

                contextual.ContextChanging += changing;
                contextual.ContextChanged += changed;
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
