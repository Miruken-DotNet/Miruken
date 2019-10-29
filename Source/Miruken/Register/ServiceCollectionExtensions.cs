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
            return AddMiruken(services, new MutableHandlerDescriptorFactory(), configure);
        }

        public static IHandler AddMiruken(
            this IServiceCollection services, IHandlerDescriptorFactory factory,
            Action<Registration> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var context = new Context();

            var registration = services.AddDefaultServices().Register(configure);
            context.AddHandlers(registration.Handlers);

            var serviceFacade = new ServiceFactoryFacade(services, factory);
            if (serviceFacade.HasServices)
                context.AddHandlers(serviceFacade);

            context.AddHandlers(new StaticHandler(), new Stash(true));

            HandlerDescriptorFactory.UseFactory(factory);

            return context;
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
            services.AddSingleton(typeof(LogFilter<,>));

            return services;
        }
    }
}
#endif