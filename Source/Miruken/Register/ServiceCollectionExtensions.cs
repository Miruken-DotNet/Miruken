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
        public static IHandler AddMiruken(
            this IServiceCollection services,
            Action<Registration> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var registration = services.AddDefaultServices().Register(configure);

            var context = new Context();
            context.AddHandlers(registration.Handlers);
            context.AddHandlers(new StaticHandler());

            var factory = new MutableHandlerDescriptorFactory();

            foreach (var service in services)
            {
                factory.RegisterService(service).Match(_ => { },
                    handler => context.AddHandlers(handler));
            }

            HandlerDescriptorFactory.UseFactory(factory);

            return context.AddHandlers(new Stash(true)).Infer();
        }

        public static IHandler AddMiruken(
            this IServiceProvider serviceProvider,
            Action<Registration> configure = null)
        {
            return new ServiceCollection().AddMiruken(
                configure + (c => c .WithServiceProvider(serviceProvider)));
        }

        public static Registration Register(this IServiceCollection services,
            Action<Registration> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var registration = new Registration(services);
            configure?.Invoke(registration);
            registration.Register();
            return registration;
        }

        public static Either<HandlerDescriptor, IHandler> RegisterService(
            this MutableHandlerDescriptorFactory factory, ServiceDescriptor service,
            HandlerDescriptorVisitor visitor = null)
        {
            var serviceType = service.ImplementationType ?? service.ServiceType;

            var instance = service.ImplementationInstance;
            if (instance != null)
            {
                if (serviceType == null)
                    serviceType = instance.GetType();

                VerifyServiceType(serviceType, service);

                var providerType = typeof(ServiceFactory<>.Instance)
                    .MakeGenericType(serviceType);

                factory.RegisterDescriptor(providerType, visitor);

                return (Handler)Activator.CreateInstance(providerType, instance);
            }

            var implementationFactory = service.ImplementationFactory;
            if (implementationFactory != null)
            {
                if (serviceType == null)
                    serviceType = implementationFactory .GetType().GenericTypeArguments[1];

                VerifyServiceType(serviceType, service);

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

                return (Handler)Activator.CreateInstance( serviceFactoryType, implementationFactory);
            }

            VerifyServiceType(serviceType, service);

            return factory.RegisterDescriptor(serviceType, 
                ServiceConfiguration.For(service) + visitor);
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

        private static void VerifyServiceType(Type serviceType, ServiceDescriptor service)
        {
            if (serviceType == null)
                throw new ArgumentException($"Unable to infer service type from descriptor {service}");

            if (serviceType == typeof(object))
                throw new ArgumentException($"Service type 'object' from descriptor {service} is not valid");
        }
    }
}