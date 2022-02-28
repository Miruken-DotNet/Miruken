namespace Miruken.Validate;

using Callback;
using Concurrency;

public static class HandlerValidateExtensions
{
    public static ValidationOutcome Validate(this IHandler handler,
        object target, params object[] scopes)
    {
        var options = handler.GetOptions<ValidationOptions>();
        var validation = new Validation(target, scopes)
        {
            StopOnFailure = options?.StopOnFailure == true
        };
        handler.Handle(validation, true);

        var outcome = validation.Outcome;
        if (target is IValidationAware validationAware)
            validationAware.ValidationOutcome = outcome;
        return outcome;
    }

    public static Promise<ValidationOutcome> ValidateAsync(
        this IHandler handler, object target, params object[] scopes)
    {
        var options    = handler.GetOptions<ValidationOptions>();
        var validation = new Validation(target, scopes)
        {
            StopOnFailure = options?.StopOnFailure == true,
            WantsAsync = true
        };
        handler.Handle(validation, true);

        return ((Promise)validation.Result).Then((r, s) =>
        {
            var outcome = validation.Outcome;
            if (target is IValidationAware validationAware)
                validationAware.ValidationOutcome = outcome;
            return outcome;
        });
    }

    public static IHandler Valid(this IHandler handler,
        object target, params object[] scopes)
    {
        return handler.Aspect((_, composer) =>
            composer.Validate(target, scopes).IsValid);
    }

    public static IHandler ValidAsync(this IHandler handler,
        object target, params object[] scopes)
    {
        return handler.Aspect((_, composer) =>
            composer.ValidateAsync(target, scopes)
                .Then((outcome, s) => outcome.IsValid));
    }
}