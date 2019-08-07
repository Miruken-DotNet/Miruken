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

        protected T CreateInstance(IHandler composer)
        {
            return (T)_factory(composer);
        }

        // Lifetime factories 

        public class Transient : ServiceFactory<T>
        {
            public Transient(Func<IServiceProvider, object> factory)
                : base(factory)
            {             
            }

            [Provides]
            public T Create(IHandler composer) => CreateInstance(composer);
        }

        public class Singleton : ServiceFactory<T>
        {
            public Singleton(Func<IServiceProvider, object> factory)
                : base(factory)
            {
            }

            [Provides, Singleton]
            public T Create(IHandler composer) => CreateInstance(composer);
        }

        public class Scoped : ServiceFactory<T>
        {
            public Scoped(Func<IServiceProvider, object> factory)
                : base(factory)
            {
            }

            [Provides, Contextual]
            public T Create(IHandler composer) => CreateInstance(composer);
        }
    }
}
