namespace Miruken.Castle
{
    using System;
    using System.Collections;
    using global::Castle.MicroKernel;
    using global::Castle.Windsor;
    using Callback;
    using global::Castle.MicroKernel.Handlers;
    using global::Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using IHandler = Callback.IHandler;

    public class WindsorHandler : Handler, IDisposable
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
        }

        public IWindsorContainer Container { get; }

        [Provides]
        private object Resolve(Inquiry inquiry, IHandler composer)
        {
            var type = inquiry.Key as Type;
            if (type == null || type.IsGenericTypeDefinition)
                return null;

            try
            {
                var args = CreateArgs(composer, inquiry);
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
        private static IDictionary CreateArgs(IHandler composer, Inquiry inquiry)
        {
            composer ??= Composer;
            return composer == null ? null : new Hashtable
            {
                [ResolutionKey] = inquiry,
                [ComposerKey]   = composer
            };
        }

        internal static readonly object ResolutionKey = new object();
        internal static readonly object ComposerKey   = new object();
    }
}
