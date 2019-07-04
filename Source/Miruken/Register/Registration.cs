namespace Miruken.Register
{
    using System;
    using System.Linq;
    using Api;
    using Api.Cache;
    using Api.Oneway;
    using Api.Route;
    using Api.Schedule;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;
    using Error;
    using Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Scrutor;

    public class Registration
    {
        private SourceSelector _from;
        private TypeSelector _select;
        private Predicate<Type> _exclude;

        public delegate IImplementationTypeSelector SourceSelector(ITypeSourceSelector source);
        public delegate void TypeSelector(IImplementationTypeSelector from);

        public Registration()
            : this(new ServiceCollection())
        {
        }

        public Registration(IServiceCollection services)
        {
            Services = services
                ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceCollection Services { get; }

        public Registration From(params SourceSelector[] from)
        {
            foreach (var source in from)
                _from += source;
            return this;
        }

        public Registration Select(params TypeSelector[] selectors)
        {
            foreach (var selector in selectors)
                _select += selector;
            return this;
        }

        public Registration Exclude(params Predicate<Type>[] excludes)
        {
            foreach (var exclude in excludes)
                _exclude += exclude;
            return this;
        }

        public IHandlerDescriptorFactory CreateFactory(IHandlerDescriptorFactory factory = null)
        {
            if (factory == null)
                factory = new MutableHandlerDescriptorFactory();

            RegisterDefaultHandlers(factory);

            if (_from != null)
            {
                Services.Scan(scan =>
                {
                    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                    foreach (SourceSelector from in _from.GetInvocationList())
                    {
                        var source = from(scan);
                        source.AddClasses(cls => cls.Where(type =>
                            !ShouldExclude(type) &&
                            (type.Is<IHandler>() || type.Is<IFilter>() ||
                             type.Is<IResolving>() || type.Name.EndsWith("Handler"))
                        )).AsSelf().WithSingletonLifetime();

                        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                        if (_select != null)
                        {
                            foreach (TypeSelector select in _select.GetInvocationList())
                                select(source);
                        }
                    }
                });
            }

            foreach (var descriptor in Services)
            {
                var implementationType = GetImplementationType(descriptor);
                if (implementationType != null)
                    factory.RegisterDescriptor(implementationType,
                        Configuration.For(descriptor));
            }

            return factory;
        }

        public IHandlerDescriptorFactory Register(IHandlerDescriptorFactory factory = null)
        {
            var f = CreateFactory(factory);
            HandlerDescriptorFactory.UseFactory(f);
            return f;
        }

        public Registration AddFilters(params IFilterProvider[] providers)
        {
            Handles.AddFilters(providers);
            return this;
        }

        public Registration AddMethodFilters(params IFilterProvider[] providers)
        {
            HandleMethodBinding.AddGlobalFilters(providers);
            return this;
        }

        private bool ShouldExclude(Type serviceType)
        {
            return _exclude?.GetInvocationList()
                       .Cast<Predicate<Type>>()
                       .Any(exclude => exclude(serviceType))
                ?? false;
        }

        private static Type GetImplementationType(ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationType != null)
                return descriptor.ImplementationType;

            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance.GetType();

            if (descriptor.ImplementationFactory != null)
            {
                var typeArguments = descriptor.ImplementationFactory
                    .GetType().GenericTypeArguments;
                if (typeArguments.Length == 2)
                    return typeArguments[1];
            }

            return null;
        }

        private static void RegisterDefaultHandlers(IHandlerDescriptorFactory factory)
        {
            factory.RegisterDescriptor<Provider>();
            factory.RegisterDescriptor<ServiceProvider>();
            factory.RegisterDescriptor<ErrorsHandler>();
            factory.RegisterDescriptor<CachedHandler>();
            factory.RegisterDescriptor<OnewayHandler>();
            factory.RegisterDescriptor<BatchRouter>();
            factory.RegisterDescriptor<PassThroughRouter>();
            factory.RegisterDescriptor<Scheduler>();
            factory.RegisterDescriptor<Stash>();
        }
    }
}
