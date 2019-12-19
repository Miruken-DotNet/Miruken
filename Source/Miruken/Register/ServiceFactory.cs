#if NETSTANDARD
namespace Miruken.Register
{
    using System;
    using Callback;
    using Context;

    [Unmanaged]
    public abstract class ServiceFactory<T> : Handler
    {
        private readonly Func<IServiceProvider, object> _factory;

        protected ServiceFactory(Func<IServiceProvider, object> factory)
        {
            _factory = factory ??
                throw new ArgumentNullException(nameof(factory));
        }

        protected T CreateInstance(Inquiry parent, IHandler composer)
        {
            return (T)_factory(new CompositionServiceProvider(parent, composer));
        }

        private class CompositionServiceProvider : IServiceProvider
        {
            private readonly Inquiry _parent;
            private readonly IHandler _composer;

            public CompositionServiceProvider(Inquiry parent, IHandler composer)
            {
                _parent   = parent;
                _composer = composer;
            }

            public object GetService(Type serviceType)
            {
                return _composer.GetService(serviceType, _parent);
            }
        }

        // Singleton Instance

        [Unmanaged]
        public class Instance : Handler
        {
            private readonly T _instance;

            public Instance(T instance)
            {
                _instance = instance;
            }

            [Provides, SkipFilters]
            public T Get() => _instance;
        }

        // Lifetime factories 

        public class Transient : ServiceFactory<T>
        {
            public Transient(Func<IServiceProvider, object> factory)
                : base(factory)
            {
            }

            [Provides, SkipFilters]
            public T Create(Inquiry inquiry, IHandler composer) =>
                CreateInstance(inquiry, composer);
        }

        public class Singleton : ServiceFactory<T>
        {
            public Singleton(Func<IServiceProvider, object> factory)
                : base(factory)
            {
            }
 
            [Provides, Contextual(Rooted = true), SkipFilters]
            public T Create(Inquiry inquiry, IHandler composer) =>
                CreateInstance(inquiry, composer);
        }

        public class Scoped : ServiceFactory<T>
        {
            public Scoped(Func<IServiceProvider, object> factory)
                : base(factory)
            {
            }
 
            [Provides, Contextual, SkipFilters]
            public T Create(Inquiry inquiry, IHandler composer) =>
                CreateInstance(inquiry, composer);
        }
    }
}
#endif