namespace Miruken.Castle
{
    using System;
    using System.Collections;
    using System.Linq;
    using global::Castle.MicroKernel;
    using global::Castle.Windsor;
    using Callback;
    using Concurrency;
    using Container;
    using global::Castle.MicroKernel.Handlers;
    using global::Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using IHandler = Callback.IHandler;

    public class WindsorHandler : Handler, IContainer, IDisposable
    {
        public WindsorHandler(Action<IWindsorContainer> config = null)
        {
            Container = new WindsorContainer();
            config?.Invoke(Container);
            ConfigureWindsor();
        }

        public WindsorHandler(IWindsorContainer container)
        {
            Container = container;
            ConfigureWindsor();
        }

        private void ConfigureWindsor()
        {
            Container.Kernel.Resolver.AddSubResolver(
                new CollectionResolver(Container.Kernel, true));
            Container.Kernel.Resolver.AddSubResolver(new CompositionResolver());
        }

        public IWindsorContainer Container { get; }

        T IContainer.Resolve<T>()
        {
            return (T)((IContainer)this).Resolve(typeof(T));
        }

        object IContainer.Resolve(object key)
        {
            var resolution = new DependencyResolution(key);
            var greedy = false;
            return Handle(resolution, ref greedy, Composer)
                 ? resolution.Result
                 : null;
        }

        Promise<T> IContainer.ResolveAsync<T>()
        {
            var container = (IContainer)this;
            return Promise.Resolved(container.Resolve<T>());
        }

        Promise IContainer.ResolveAsync(object key)
        {
            var container = (IContainer)this;
            return Promise.Resolved(container.Resolve(key));
        }

        T[] IContainer.ResolveAll<T>()
        {
            return ((IContainer)this).ResolveAll(typeof(T))
                .Cast<T>().ToArray();
        }

        object[] IContainer.ResolveAll(object key)
        {
            var resolution = new DependencyResolution(key, null, true);
            var greedy = true;
            return Handle(resolution, ref greedy, Composer)
                 ? (object[])resolution.Result
                 : Array.Empty<object>();
        }

        Promise<T[]> IContainer.ResolveAllAsync<T>()
        {
            var container = (IContainer)this;
            return Promise.Resolved(container.ResolveAll<T>());
        }

        Promise<object[]> IContainer.ResolveAllAsync(object key)
        {
            var container = (IContainer)this;
            return Promise.Resolved(container.ResolveAll(key));
        }

        void IContainer.Release(object component)
        {
            Container.Release(component);
        }

        [Provides]
        private object Resolve(Inquiry inquiry, IHandler composer)
        {
            var type = inquiry.Key as Type;
            if (type == null || type.IsGenericTypeDefinition)
                return null;

            var dependency = inquiry as DependencyResolution
                          ?? new DependencyResolution(inquiry.Key);

            if (!dependency.Claim(this))
                return null;  // cycle detected

            try
            {
                var args = CreateArgs(composer, dependency);
                return inquiry.Many || inquiry is Resolving
                     ? Container.ResolveAll(type, args)
                     : Resolve(type, args);
            }
            catch (ComponentNotFoundException)
            {
                return null;
            }
        }

        public void Dispose()
        {
            Container?.Dispose();
        }

        private object Resolve(Type type, IDictionary args)
        {
            try
            {
                return Container.Resolve(type, args);
            }
            catch (GenericHandlerTypeMismatchException)
            {
                // Generic type constraints are not handled out
                // of the box.  IHandlerSelector can be used
                // but will throw this exception if no handlers
                // are returned.
                return null;
            }
        }
        private static IDictionary CreateArgs(
            IHandler composer, DependencyResolution resolution)
        {
            composer = composer ?? Composer;
            return composer == null ? null : new Hashtable
            {
                [ResolutionKey] = resolution,
                [ComposerKey]   = composer
            };
        }

        internal static readonly object ResolutionKey = new object();
        internal static readonly object ComposerKey   = new object();
    }
}
