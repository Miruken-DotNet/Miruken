namespace Miruken.Register
{
    using System;
    using Api;
    using Callback;
    using Context;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static IHandler AddMiruken(
            this IServiceCollection services,
            Action<Registration> configure = null)
        {
            var registration = new Registration(services);
            configure?.Invoke(registration);
            registration.Register();

            return (new Stash(true) + new Context()
                        .AddHandlers(new StaticHandler()))
                .Infer();
        }
    }
}
