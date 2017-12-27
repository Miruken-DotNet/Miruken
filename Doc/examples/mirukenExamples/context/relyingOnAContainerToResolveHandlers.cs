namespace Example.MirukenExamples.Context
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
            return (T) Resolve(typeof(T));
        }

        private object Resolve(Type type)
        {
            if (type == typeof(SomeHandler))
                return new SomeHandler();

            if (type == typeof(AnotherHandler))
                return new AnotherHandler();

            throw new ArgumentException("Unknown type");
        }

//endExample

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

        public Promise<object[]> ResolveAllAsync(object ke)
        {
            throw new NotImplementedException();
        }

        public void Release(object component)
        {
            throw new NotImplementedException();
        }
    }
}
