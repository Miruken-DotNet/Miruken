﻿namespace Miruken.Configuration
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Register;

    public static class RegistrationExtensions
    {
        public static Registration WithTypedConfiguration(
            this Registration registration, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (!registration.CanRegister(typeof(RegistrationExtensions)))
                return registration;

            registration.Services(services => registration
                .Select((selector, publicOnly) =>
                    selector.AddClasses(x => x.Where(type =>
                    {
                        if (type
                            .GetCustomAttributes(typeof(ConfigurationAttribute), true)
                            .FirstOrDefault() is ConfigurationAttribute attribute)
                        {
                            var ctor = type.GetConstructor(Type.EmptyTypes);
                            if (ctor == null) return false;
                            var instance = ctor.Invoke(new object[0]);
                            var key      = GetConfigurationKey(attribute, type);
                            services.AddSingleton(type, provider =>
                            {
                                configuration.GetSection(key).Bind(instance);
                                return instance;
                            });
                        }
                        return false;
                    }), publicOnly)));

            return registration;
        }

        private static string GetConfigurationKey(ConfigurationAttribute attribute, Type type)
        {
            var key = attribute.Key;
            if (!string.IsNullOrEmpty(key)) return key;
            key = type.Name;
            if (key.EndsWith("Config"))
                key = key.Remove(key.Length - 6);
            else if (key.EndsWith("Configuration"))
                key = key.Remove(key.Length - 13);
            return key;
        }
    }
}
