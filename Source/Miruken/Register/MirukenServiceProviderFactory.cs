#if NETSTANDARD
namespace Miruken.Register
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class MirukenServiceProviderFactory : IServiceProviderFactory<Registration>
    {
        private readonly Action<Registration> _configure;

        public MirukenServiceProviderFactory(Action<Registration> configure = null)
        {
            _configure = configure;
        }

        public Registration CreateBuilder(IServiceCollection services)
        {
            var registration = services.GetRegistration() ?? new Registration(services);
            _configure?.Invoke(registration);
            return registration;
        }

        public IServiceProvider CreateServiceProvider(Registration registration)
        {
            return registration.Build();
        }
    }
}
#endif
