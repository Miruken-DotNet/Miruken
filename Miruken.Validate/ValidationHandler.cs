namespace Miruken.Validate
{
    using Callback;
    using Concurrency;

    public class ValidationHandler : Handler, IValidator
    {
        public ValidationOutcome Validate(
            object target, params object[] scopes)
        {
            var validation = new Validation(target, scopes);
            Composer.Handle(validation, true);

            var outcome         = validation.Outcome;
            var validationAware = target as IValidationAware;
            if (validationAware != null)
                validationAware.ValidationOutcome = outcome;
            return outcome;
        }

        public Promise<ValidationOutcome> ValidateAsync(
            object target, params object[] scopes)
        {
            var validation = new Validation(target, scopes) { WantsAsync = true };
            Composer.Handle(validation, true);

            return ((Promise)validation.Result).Then((r, s) =>
            {
                var outcome         = validation.Outcome;
                var validationAware = target as IValidationAware;
                if (validationAware != null)
                    validationAware.ValidationOutcome = outcome;
                return outcome;
            });
        }
    }

    public static class ValidationExtensions
    {
        public static IHandler Valid(
            this IHandler handler, object target, params object[] scopes)
        {
            return handler.Aspect((_, composer) =>
                composer.P<IValidator>().Validate(target, scopes)
                        .IsValid);
        }

        public static IHandler ValidAsync(
             this IHandler handler, object target, params object[] scopes)
        {
            return handler.Aspect((_, composer) =>
                composer.P<IValidator>().ValidateAsync(target, scopes)
                    .Then((outcome, s) => outcome.IsValid));
        }
    }
}
