namespace Miruken.Validate
{
    using Callback;
    using Concurrency;

    public class ValidationHandler : Handler, IValidating
    {
        public ValidationOutcome Validate(
            object target, params object[] scopes)
        {
            var composer   = Composer;
            var options    = GetOptions(composer);
            var validation = new Validation(target, scopes)
            {
                StopOnFailure = options?.StopOnFailure == true
            };
            composer.Handle(validation, true);

            var outcome         = validation.Outcome;
            var validationAware = target as IValidationAware;
            if (validationAware != null)
                validationAware.ValidationOutcome = outcome;
            return outcome;
        }

        public Promise<ValidationOutcome> ValidateAsync(
            object target, params object[] scopes)
        {
            var composer   = Composer;
            var options    = GetOptions(composer);
            var validation = new Validation(target, scopes)
            {
                StopOnFailure = options?.StopOnFailure == true,
                WantsAsync    = true
            };
            composer.Handle(validation, true);

            return ((Promise)validation.Result).Then((r, s) =>
            {
                var outcome         = validation.Outcome;
                var validationAware = target as IValidationAware;
                if (validationAware != null)
                    validationAware.ValidationOutcome = outcome;
                return outcome;
            });
        }

        private static ValidationOptions GetOptions(IHandler composer)
        {
            var options = new ValidationOptions();
            return composer.Handle(options) ? options : null;
        }
    }

    public static class ValidationExtensions
    {
        public static IHandler Valid(
            this IHandler handler, object target, params object[] scopes)
        {
            return handler.Aspect((_, composer) =>
                composer.Proxy<IValidating>().Validate(target, scopes)
                        .IsValid);
        }

        public static IHandler ValidAsync(
             this IHandler handler, object target, params object[] scopes)
        {
            return handler.Aspect((_, composer) =>
                composer.Proxy<IValidating>().ValidateAsync(target, scopes)
                    .Then((outcome, s) => outcome.IsValid));
        }
    }
}
