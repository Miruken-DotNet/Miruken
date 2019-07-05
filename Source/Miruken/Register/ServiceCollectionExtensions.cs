namespace Miruken.Register
{
    using System;
    using Callback;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static IHandler AddMiruken(
            this IServiceCollection services, Action<Registration> configure = null,
            Func<IServiceCollection, IServiceProvider> serviceProviderFactory = null)
        {
            var provider     = serviceProviderFactory?.Invoke(services);
            var registration = provider == null
                             ? new Registration(services)
                             : new Registration();
            configure?.Invoke(registration);
            registration.Register();

            return new StaticHandler().Provider(provider).Infer();
        }
    }
}
