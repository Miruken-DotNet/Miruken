#if NETSTANDARD
namespace Miruken.Register
{
    using System;
    using Callback.Policy;
    using Microsoft.Extensions.DependencyInjection;

    public class MirukenServiceProviderFactory : IServiceProviderFactory<Registration>
    {
        private readonly Action<Registration> _configure;
        private readonly IHandlerDescriptorFactory _factory;

        public MirukenServiceProviderFactory(
            IHandlerDescriptorFactory factory = null)
            : this(null, factory)
        {
        }

        public MirukenServiceProviderFactory(
            Action<Registration> configure,
            IHandlerDescriptorFactory factory = null)
        {
            _configure = configure;
            _factory   = factory;
        }

        public Registration CreateBuilder(IServiceCollection services)
        {
            var registration = services.GetRegistration() ?? new Registration(services);
            _configure?.Invoke(registration);
            return registration;
        }

        public IServiceProvider CreateServiceProvider(Registration registration)
        {
            return registration.Build(_factory);
        }
    }
}
#endif
