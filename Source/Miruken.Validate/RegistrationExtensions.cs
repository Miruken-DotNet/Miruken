namespace Miruken.Validate
{
    using global::FluentValidation;
    using Register;

    public static class RegistrationExtensions
    {
        public static Registration WithValidation(this Registration registration)
        {
            return registration.CanRegister(typeof(RegistrationExtensions))
                 ? registration
                     .Sources(sources => sources.FromAssemblyOf<Validation>())
                     .Select((source, publicOnly) =>
                         source.AddClasses(x => x.AssignableTo<IValidator>(), publicOnly))
                 : registration;
        }
    }
}
