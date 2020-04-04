namespace Miruken.Register
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static Registration AddMiruken(
            this IServiceCollection services, Action<Registration> configure = null)
        {
            var registration = new Registration(services);
            configure?.Invoke(registration);
            return registration;
        }

        public static Registration GetRegistration(this IServiceCollection services)
        {
            return services
                .Select(s => s.ImplementationInstance).OfType<Registration>()
                .FirstOrDefault();
        }
    }
}
