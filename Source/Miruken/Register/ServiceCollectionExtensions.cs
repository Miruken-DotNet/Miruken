#if NETSTANDARD
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
    using Log;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static IHandler AddMiruken(
            this IServiceCollection services, Action<Registration> configure = null)
        {
            return AddMiruken(services, null, configure);
        }

        public static IHandler AddMiruken(
            this IServiceCollection services, IHandlerDescriptorFactory factory,
            Action<Registration> configure = null)
        {
            var registration = new Registration(services);
            configure?.Invoke(registration);
            return registration.AddMiruken(factory);
        }

        public static IHandler AddMiruken(this Registration registration)
        {
            return AddMiruken(registration, null);
        }

        public static IHandler AddMiruken(
            this Registration registration, IHandlerDescriptorFactory factory)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));

            factory = factory ?? new MutableHandlerDescriptorFactory();

            var (@explicit, @implicit) = registration.Register();

            foreach (var service in @implicit.AddDefaultServices())
            {
                var serviceType = service.ImplementationType ?? service.ServiceType;
                factory.RegisterDescriptor(serviceType, ServiceConfiguration.For(service));
            }

            var context = new Context();
            context.AddHandlers(registration.Handlers);

            var serviceFacade = new ServiceFactoryFacade(@explicit, factory);
            if (serviceFacade.HasServices) context.AddHandlers(serviceFacade);

            context.AddHandlers((new StaticHandler() + new Stash(true)).Break());

            HandlerDescriptorFactory.UseFactory(factory);

            return context;
        }

        private static IServiceCollection AddDefaultServices(this IServiceCollection services)
        {
            services.AddTransient<Provider>();
            services.AddTransient<ServiceProvider>();
            services.AddTransient<ServiceFactoryFacade>();
            services.AddTransient<BatchRouter>();
            services.AddTransient<Stash>();

            services.AddSingleton<ErrorsHandler>();
            services.AddSingleton<CachedHandler>();
            services.AddSingleton<OnewayHandler>();
            services.AddSingleton<OnceHandler>();
            services.AddSingleton<PassThroughRouter>();
            services.AddSingleton<Scheduler>();

            services.AddSingleton<LoggerProvider>();
            services.AddSingleton(typeof(LogFilter<,>));

            return services;
        }
    }
}
#endif