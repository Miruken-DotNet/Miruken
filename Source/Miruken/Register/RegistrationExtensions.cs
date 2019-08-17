namespace Miruken.Register
{
    using System;
    using Callback;

    public static class RegistrationExtensions
    {
        public static Registration With(
            this Registration registration, object value)
        {
            return registration.AddHandlers(new Provider(value));
        }

        public static Registration WithServiceProvider(
            this Registration registration, IServiceProvider serviceProvider)
        {
            return registration.AddHandlers(new ServiceProvider(serviceProvider));
        }
    }
}
