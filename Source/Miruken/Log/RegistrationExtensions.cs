namespace Miruken.Log
{
    using Register;

    public static class RegistrationExtensions
    {
        public static Registration WithLogging(this Registration registration)
        {
            if (!registration.CanRegister(typeof(RegistrationExtensions)))
                return registration;

            registration.AddFilters(new LogAttribute());

            return registration;
        }
    }
}

