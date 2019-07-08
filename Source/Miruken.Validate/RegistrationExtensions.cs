namespace Miruken.Validate
{
    using global::FluentValidation;
    using Register;

    public static class RegistrationExtensions
    {
        public static Registration WithValidation(this Registration registration)
        {
            return registration.From(sources => sources.FromAssemblyOf<Validation>())
                .Select((from, publicOnly) => 
                    from.AddClasses(x => x.AssignableTo<IValidator>(), publicOnly));
        }
    }
}
