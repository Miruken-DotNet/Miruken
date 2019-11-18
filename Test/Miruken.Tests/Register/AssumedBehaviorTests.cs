#if NETSTANDARD
namespace Miruken.Tests.Register
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ServiceCollection = Miruken.Register.ServiceCollection;

    public abstract class AssumedBehaviorTests
    {
        [TestMethod]
        public void WillNotResolveEmptyArrayDependency()
        {
            var services     = new ServiceCollection().AddSingleton<Bar>();
            var rootProvider = CreateServiceProvider(services);
            var bar          = rootProvider.GetService<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.Foos.Any());
        }

        [TestMethod]
        public void DisposingScopeAlsoDisposesServiceProvider()
        {
            // You can't resolve things from a scope's service provider
            // if you dispose the scope.
            var services     = new ServiceCollection().AddScoped<DisposeTracker>();
            var rootProvider = CreateServiceProvider(services);
            var scope        = rootProvider.CreateScope();
            Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<DisposeTracker>());
            scope.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => scope.ServiceProvider.GetRequiredService<DisposeTracker>());
        }

        [TestMethod]
        public void DisposingScopeAndProviderOnlyDisposesObjectsOnce()
        {
            // Disposing the service provider and then the scope only
            // runs one disposal on the resolved objects.
            var services     = new ServiceCollection().AddScoped<DisposeTracker>();
            var rootProvider = CreateServiceProvider(services);
            var scope        = rootProvider.CreateScope();
            var tracker      = scope.ServiceProvider.GetRequiredService<DisposeTracker>();
            ((IDisposable)scope.ServiceProvider).Dispose();
            Assert.IsTrue(tracker.Disposed);
            Assert.AreEqual(1, tracker.DisposeCount);
            scope.Dispose();
            Assert.AreEqual(1, tracker.DisposeCount);
        }

        [TestMethod]
        public void DisposingScopeServiceProviderStopsNewScopes()
        {
            // You can't create a new child scope if you've disposed of
            // the parent scope service provider.
            var rootProvider = CreateServiceProvider(new ServiceCollection());
            var scope        = rootProvider.CreateScope();
            ((IDisposable)scope.ServiceProvider).Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => scope.ServiceProvider.CreateScope());
        }

        [TestMethod]
        public void DisposingScopeServiceProviderStopsScopeResolutions()
        {
            // You can't resolve things from a scope if you dispose the
            // scope's service provider.
            var services     = new ServiceCollection().AddScoped<DisposeTracker>();
            var rootProvider = CreateServiceProvider(services);
            var scope        = rootProvider.CreateScope();
            Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<DisposeTracker>());
            ((IDisposable)scope.ServiceProvider).Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => scope.ServiceProvider.GetRequiredService<DisposeTracker>());
        }

        [TestMethod]
        public void ResolvedProviderNotSameAsParent()
        {
            // Resolving a provider from another provider yields a new object.
            // (It's not just returning "this" - it's a different IServiceProvider.)
            var parent   = CreateServiceProvider(new ServiceCollection());
            var resolved = parent.GetRequiredService<IServiceProvider>();
            Assert.AreNotSame(parent, resolved);
        }

        [TestMethod]
        public void ResolvedProviderUsesSameScopeAsParent()
        {
            // Resolving a provider from another provider will still resolve
            // items from the same scope.
            var services = new ServiceCollection().AddScoped<DisposeTracker>();
            var root     = CreateServiceProvider(services);
            var scope    = root.CreateScope();
            var parent   = scope.ServiceProvider;
            var resolved = parent.GetRequiredService<IServiceProvider>();
            Assert.AreSame(parent.GetRequiredService<DisposeTracker>(), resolved.GetRequiredService<DisposeTracker>());
        }

        [TestMethod]
        public void ServiceProviderWillNotResolveAfterDispose()
        {
            // You can't resolve things from a service provider
            // if you dispose it.
            var services     = new ServiceCollection().AddScoped<DisposeTracker>();
            var rootProvider = CreateServiceProvider(services);
            Assert.IsNotNull(rootProvider.GetRequiredService<DisposeTracker>());
            ((IDisposable)rootProvider).Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => rootProvider.GetRequiredService<DisposeTracker>());
        }

        protected abstract IServiceProvider CreateServiceProvider(
            IServiceCollection serviceCollection);

        private class Foo { }

        private class Bar
        {
            public Bar(IEnumerable<Foo> foos)
            {
                Foos = foos;
            }

            public IEnumerable<Foo> Foos { get; }
        }

        private class DisposeTracker : IDisposable
        {
            public int DisposeCount { get; private set; }

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
                DisposeCount++;
            }
        }
    }
}
#endif