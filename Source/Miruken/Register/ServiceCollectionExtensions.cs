namespace Miruken.Register
{
    using System;
    using Api;
    using Api.Cache;
    using Api.Once;
    using Api.Oneway;
    using Api.Route;
    using Api.Schedule;
    using Callback;
    using Callback.Policy;
    using Context;
    using Error;
    using Functional;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Register(
            this IServiceCollection services,
            Action<Registration> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var registration = new Registration(services);
            configure?.Invoke(registration);
            return registration.Register();
        }

        public static IHandler AddMiruken(
            this IServiceCollection services,
            Action<Registration> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var context = new Context();
            context.AddHandlers(new StaticHandler());

            services.AddDefaultServices().Register(configure);

            var factory = new MutableHandlerDescriptorFactory();

            foreach (var service in services)
            {
                factory.RegisterService(service).Match(_ => { },
                    handler => context.AddHandlers(handler));
            }

            HandlerDescriptorFactory.UseFactory(factory);

            return (context + new Stash(true)).Infer();
        }

        public static Either<HandlerDescriptor, IHandler> RegisterService(
            this MutableHandlerDescriptorFactory factory, ServiceDescriptor service,
            HandlerDescriptorVisitor visitor = null)
        {
            var serviceType = service.ImplementationType ?? service.ServiceType;

            if (service.ImplementationInstance != null)
            {
                if (serviceType == null)
                    serviceType = service.ImplementationInstance.GetType();

                AssertValidServiceType(serviceType, service);

                var providerType = typeof(InstanceProvider<>)
                    .MakeGenericType(serviceType);

                factory.RegisterDescriptor(providerType, visitor);

                return (Handler)Activator.CreateInstance(
                    providerType, service.ImplementationInstance);
            }

            if (service.ImplementationFactory != null)
            {
                if (serviceType == null)
                {
                    serviceType = service.ImplementationFactory
                        .GetType().GenericTypeArguments[1];
                }

                AssertValidServiceType(serviceType, service);

                Type serviceFactoryType;
                switch (service.Lifetime)
                {
                    case ServiceLifetime.Transient:
                        serviceFactoryType = typeof(ServiceFactory<>.Transient);
                        break;
                    case ServiceLifetime.Singleton:
                        serviceFactoryType = typeof(ServiceFactory<>.Singleton);
                        break;
                    case ServiceLifetime.Scoped:
                        serviceFactoryType = typeof(ServiceFactory<>.Scoped);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported lifetime {service.Lifetime}");
                }

                serviceFactoryType = serviceFactoryType.MakeGenericType(serviceType);

                factory.RegisterDescriptor(serviceFactoryType, visitor);

                return (Handler)Activator.CreateInstance(
                    serviceFactoryType, service.ImplementationFactory);
            }

            AssertValidServiceType(serviceType, service);
            visitor = ServiceConfiguration.For(service) + visitor;
            return factory.RegisterDescriptor(serviceType, visitor);
        }

        public static IServiceCollection AddDefaultServices(this IServiceCollection services)
        {
            services.AddTransient<Provider>();
            services.AddTransient<ServiceProvider>();
            services.AddTransient<BatchRouter>();
            services.AddTransient<Stash>();

            services.AddSingleton<ErrorsHandler>();
            services.AddSingleton<CachedHandler>();
            services.AddSingleton<OnewayHandler>();
            services.AddSingleton<OnceHandler>();
            services.AddSingleton<PassThroughRouter>();
            services.AddSingleton<Scheduler>();

            return services;
        }

        private static void AssertValidServiceType(Type serviceType, ServiceDescriptor service)
        {
            if (serviceType == null)
                throw new ArgumentException($"Unable to infer service type from descriptor {service}");

            if (serviceType == typeof(object))
                throw new ArgumentException($"Service type object from descriptor {service} is not valid");
        }
    }
}
