namespace Miruken.Register
{
    using System;
    using Api;
    using Api.Cache;
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
            services.AddDefaultServices()
                    .Register(configure);

            HandlerDescriptorFactory.UseFactory(services.CreateHandlerDescriptorFactory());

            return (new Stash(true) + new Context()
                        .AddHandlers(new StaticHandler()))
                .Infer();
        }

        public static IHandlerDescriptorFactory CreateHandlerDescriptorFactory(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            return new ImmutableHandlerDescriptorFactory(services);
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
            services.AddSingleton<PassThroughRouter>();
            services.AddSingleton<Scheduler>();

            return services;
        }
    }
}
