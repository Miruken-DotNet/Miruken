namespace Examples.MirukenExamples.Context
{
    using System;
    using Miruken.Callback;
    using Miruken.Concurrency;
    using Miruken.Container;
    using Miruken.Context;

    public class RelyingOnAContainerToResolveHandlers
    {
        public Context Context { get; set; }

        public RelyingOnAContainerToResolveHandlers()
        {
            Context = new Context();
            Context
                .AddHandlers(new ContainerHandler())
                .AddHandler<SomeHandler>()
                .AddHandler<AnotherHandler>();
        }
    }

    public class ContainerHandler: Handler, IContainer
    {
        public T Resolve<T>()
        {
            object instance;

            var type = typeof(T);
            if (type == typeof(SomeHandler))
                instance = new SomeHandler();
            else if (type == typeof(AnotherHandler))
                instance = new AnotherHandler();
            else
                throw new ArgumentException("Unknown type");

            return (T) instance;
        }

        public object Resolve(Type type)
        {
            throw new NotImplementedException();

        }

        public object Resolve(object key)
        {
            throw new NotImplementedException();

        }

        public Promise<T> ResolveAsync<T>()
        {
            throw new NotImplementedException();
        }

        public Promise ResolveAsync(object key)
        {
            throw new NotImplementedException();
        }

        public T[] ResolveAll<T>()
        {
            throw new NotImplementedException();
        }

        public object[] ResolveAll(object key)
        {
            throw new NotImplementedException();
        }

        public Promise<T[]> ResolveAllAsync<T>()
        {
            throw new NotImplementedException();
        }

        public Promise ResolveAllAsync(object ke)
        {
            throw new NotImplementedException();
        }

        public void Release(object component)
        {
            throw new NotImplementedException();
        }
    }
}
