﻿namespace Miruken.Validate
{
    using global::FluentValidation;
    using Register;

    public static class RegistrationExtensions
    {
        public static Registration WithValidation(this Registration registration)
        {
            if (!registration.CanRegister(typeof(RegistrationExtensions)))
                return registration;

            return registration
                .Sources(sources => sources.FromAssemblyOf<Validation>())
                .Select((source, publicOnly) =>
                    source.AddClasses(x => x.AssignableTo<IValidator>(), publicOnly));
        }
    }
}
