namespace Miruken.Validate.FluentValidation
{
    using System;
    using System.Threading.Tasks;
    using Callback;
    using global::FluentValidation;
    using global::FluentValidation.Results;
    using Infrastructure;

    public class FluentValidationValidator : Handler
    {
        [Provides, Singleton]
        public FluentValidationValidator()
        {        
        }

        [Validates(Scope = Scopes.Any)]
        public async Task Validate<T>(T target, Validation validation, IHandler composer)
        {
            var validators = composer.ResolveAll<IValidator<T>>();
            if (validators.Length == 0) return;

            var outcome = validation.Outcome;
            var scope   = validation.ScopeMatcher;
            var context = scope != null && scope != EqualsScopeMatcher.Default
                ? new ValidationContext<T>(target, null, new ScopeSelector(scope))
                : new ValidationContext<T>(target);
            context.SetValidation(validation);
            context.SetComposer(composer);

            Array.Sort(validators, OrderedComparer<IValidator<T>>.Instance);

            ValidationResult result = null;
            foreach (var validator in validators)
            {
                result = await validator.ValidateAsync(context).ConfigureAwait(false);
                if (!result.IsValid && validation.StopOnFailure)
                    break;
            }
            
            AddErrors(result, outcome);
        }

        private static void AddErrors(ValidationResult result, ValidationOutcome outcome)
        {
            foreach (var error in result.Errors)
            {
                var failure = error.CustomState as ValidationOutcome ??
                              (object)error.ErrorMessage;
                outcome.AddError(error.PropertyName, failure);
            }
        }
    }

    public static class FluentValidatorExtensions
    {
        private const string ComposerKey = "Miruken.Composer";
        private const string ValidationKey = "Miruken.Validation";

        public static void SetValidation(
            this IValidationContext context, Validation validation)
        {
            context.RootContextData[ValidationKey] = validation;
        }

        public static Validation GetValidation(this IValidationContext context)
        {
            return context.RootContextData.TryGetValue(ValidationKey, out var validation)
                 ? (Validation)validation
                 : null;
        }

        public static void SetComposer(this IValidationContext context, IHandler composer)
        {
            context.RootContextData[ComposerKey] = composer;
        }

        public static IHandler GetComposer(this IValidationContext context)
        {
            return context.RootContextData.TryGetValue(ComposerKey, out var composer)
                 ? (IHandler)composer
                 : null;
        }

        public static IRuleBuilderOptions<T, TProp> Valid<T, TProp>(
            this IRuleBuilder<T, TProp> builder)
        {
            return builder.SetValidator(FluentValidator<TProp>.Instance);
        }
    }
}
