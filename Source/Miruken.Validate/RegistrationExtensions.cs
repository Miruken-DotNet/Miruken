namespace Miruken.Validate
{
    using System;
    using global::FluentValidation;
    using Register;

    public static class RegistrationExtensions
    {
        public static Registration WithValidation(this Registration registration,
            Action<ValidateAttribute> configure = null)
        {
            if (!registration.CanRegister(typeof(RegistrationExtensions)))
                return registration;

            var attribute = new ValidateAttribute();
            configure?.Invoke(attribute);
            registration.AddFilters(attribute);

            return registration
                .Sources(sources => sources.FromAssemblyOf<Validation>())
                .Select((selector, publicOnly) =>
                    selector.AddClasses(x => x.AssignableTo<IValidator>(), publicOnly)
                        .AsSelf().WithSingletonLifetime());
        }
    }
}

