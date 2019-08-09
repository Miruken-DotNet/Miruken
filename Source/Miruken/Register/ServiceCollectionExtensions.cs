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

            services.AddDefaultServices().Register(configure);

            var factory = new MutableHandlerDescriptorFactory();

            foreach (var service in services)
                factory.RegisterService(service);

            HandlerDescriptorFactory.UseFactory(factory);

            return new Context().AddHandlers(new StaticHandler(), new Stash(true)).Infer();
        }

        public static HandlerDescriptor RegisterService(
            this MutableHandlerDescriptorFactory factory, ServiceDescriptor service,
            HandlerDescriptorVisitor visitor = null)
        {
            var serviceType = service.ImplementationType ?? service.ServiceType;

            var instance = service.ImplementationInstance;
            if (instance != null)
            {
                if (serviceType == null)
                    serviceType = instance.GetType();

                return factory.RegisterServiceFactory(
                    service, serviceType, _ => instance, visitor);
            }

            var implementationFactory = service.ImplementationFactory;
            if (implementationFactory != null)
            {
                if (serviceType == null)
                    serviceType = implementationFactory .GetType().GenericTypeArguments[1];

                return factory.RegisterServiceFactory(
                    service, serviceType, implementationFactory, visitor);
            }

            AssertValidServiceType(serviceType, service);

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

        private static HandlerDescriptor RegisterServiceFactory(
            this IHandlerDescriptorFactory factory,
            ServiceDescriptor service, Type serviceType,
            Func<IServiceProvider, object> serviceFactory,
            HandlerDescriptorVisitor visitor)
        {
            AssertValidServiceType(serviceType, service);

            serviceType = typeof(ServiceFactory<>).MakeGenericType(serviceType);

            visitor += (d, binding) =>
            {
                if (binding.Dispatcher.Member.Name == nameof(ServiceFactory<object>.Create))
                    binding.AddFilters(new ServiceFactoryProvider(serviceFactory));
            };

            return factory.RegisterDescriptor(serviceType,
                ServiceConfiguration.For(service) + visitor);
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