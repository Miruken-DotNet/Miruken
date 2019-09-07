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
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static IHandler AddMiruken(
            this IServiceCollection services,
            Action<Registration> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var context = new Context();
            var factory = new MutableHandlerDescriptorFactory();

            var registration = services.AddDefaultServices().Register(configure);
            context.AddHandlers(registration.Handlers);

            foreach (var service in services)
                RegisterService(context, factory, service);

            context.AddHandlers(new StaticHandler(), new Stash(true));

            HandlerDescriptorFactory.UseFactory(factory);

            return context.Infer();
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

        private static void RegisterService(Context context,
            IHandlerDescriptorFactory factory, ServiceDescriptor service,
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

                var handler = (Handler)Activator.CreateInstance(providerType, instance);
                context.AddHandlers(handler);
                return;
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

                var handler = (Handler)Activator.CreateInstance(serviceFactoryType, implementationFactory);
                context.AddHandlers(handler);
                return;
            }

            VerifyServiceType(serviceType, service);

            factory.RegisterDescriptor(serviceType, ServiceConfiguration.For(service) + visitor);
        }

        private static IServiceCollection AddDefaultServices(this IServiceCollection services)
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

            services.AddSingleton(typeof(ServiceFactory<>.Instance));
            services.AddSingleton(typeof(ServiceFactory<>.Transient));
            services.AddSingleton(typeof(ServiceFactory<>.Singleton));
            services.AddSingleton(typeof(ServiceFactory<>.Scoped));

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