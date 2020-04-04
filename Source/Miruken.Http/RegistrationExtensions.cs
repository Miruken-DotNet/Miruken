namespace Miruken.Http
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Register;

    public static class RegistrationExtensions
    {
        public static Registration WithHttp(this Registration registration,
            Action<IHttpClientBuilder> build = null)
        {
            if (!registration.CanRegister(typeof(RegistrationExtensions)))
                return registration;

            return registration.Services(services =>
            {
                var httpBuilder = services
                    .AddTransient<HttpOptionsHandler>()
                    .AddHttpClient<HttpService>()
                    .AddHttpMessageHandler<HttpOptionsHandler>();
                build?.Invoke(httpBuilder);
            }).Sources(sources => sources.FromAssemblyOf<HttpService>());
        }
    }
}

