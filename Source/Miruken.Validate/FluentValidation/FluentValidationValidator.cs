namespace Miruken.Validate.FluentValidation
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Callback;
    using global::FluentValidation;
    using global::FluentValidation.Results;

    public class FluentValidationValidator : Handler
    {
        [Validates(Scope = Scopes.Any)]
        public async Task Validate<T>(T target, Validation validation, IHandler composer)
        {
            Console.WriteLine("Fluent");
            var validators = composer.ResolveAll<IValidator<T>>();
            if (validators.Length == 0) return;
            Console.WriteLine($"Fluent Validators{target} {validators.Length}");

            var outcome = validation.Outcome;
            var scope   = validation.ScopeMatcher;
            var context = scope != null && scope != EqualsScopeMatcher.Default
                ? new ValidationContext(target, null, new ScopeSelector(scope)) 
                : new ValidationContext(target);
            context.SetValidation(validation);
            context.SetComposer(composer);

            foreach (var validator in validators)
            {
                Console.WriteLine($"Fluent Validator {validator}");
                var result = await validator.ValidateAsync(context)
                    .ConfigureAwait(false);
                Console.WriteLine($"Fluent valid {result.IsValid}");
                if (!result.IsValid)
                {
                    AddErrors(result, outcome);
                    if (validation.StopOnFailure)
                        break;
                }
            }
        }

        private static void AddErrors(ValidationResult result, ValidationOutcome outcome)
        {
            foreach (var error in result.Errors)
            {
                var child   = error as OutcomeFailure;
                var failure = child?.FailedOutcome ?? (object)error.ErrorMessage;
                outcome.AddError(error.PropertyName, failure);
            }
        }
    }

    public static class FluentValidatorExtensions
    {
        private const string ComposerKey   = "Miruken.Composer";
        private const string ValidationKey = "Miruken.Validation";

        public static void SetValidation(
            this ValidationContext context, Validation validation)
        {
            context.RootContextData[ValidationKey] = validation;
        }

        public static Validation GetValidation(this ValidationContext context)
        {
            object validation;
            return context.RootContextData.TryGetValue(ValidationKey, out validation)
                ? (Validation)validation
                : null;
        }

        public static void SetComposer(this ValidationContext context, IHandler composer)
        {
            context.RootContextData[ComposerKey] = composer;
        }

        public static IHandler GetComposer(this ValidationContext context)
        {
            object composer;
            return context.RootContextData.TryGetValue(ComposerKey, out composer)
                ? (IHandler) composer
                : null;
        }

        public static IRuleBuilderOptions<T, TProp> Valid<T, TProp>(
            this IRuleBuilder<T, TProp> builder)
        {
            return builder.SetValidator(FluentValidator<TProp>.Instance);
        }

        public static CollectionValidatorExtensions
            .ICollectionValidatorRuleBuilder<T, TElem> ValidCollection<T, TElem>(
            this IRuleBuilder<T, IEnumerable<TElem>> builder)
        {
            return builder.SetCollectionValidator(FluentValidator<TElem>.Instance);
        }
    }
}
