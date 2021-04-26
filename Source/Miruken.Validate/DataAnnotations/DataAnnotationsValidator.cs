namespace Miruken.Validate.DataAnnotations
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Callback;

    public class DataAnnotationsValidator : Handler
    {
        [Provides, Singleton]
        public DataAnnotationsValidator()
        {         
        }

        [Validates(Scope = Scopes.Any)]
        public void Validate(Validation validation, IHandler composer)
        {
            var target  = validation.Target;
            var outcome = validation.Outcome;
            var results = new List<ValidationResult>();
            var context = new ValidationContext(target, composer, null);
            context.SetValidation(validation);
            context.SetComposer(composer);

            var isValid = Validator.TryValidateObject(target, context, results, true);
            if (isValid) return;

            foreach (var result in results)
            {
                if (result is not CompositeResult compositeResult)
                {
                    var error = result.ErrorMessage;
                    foreach (var memberName in result.MemberNames)
                        outcome.AddError(memberName, error);
                }
                else
                {
                    foreach (var memberName in result.MemberNames)
                        outcome.AddError(memberName, compositeResult.Outcome);
                }
            }
        }
    }

    public static class DataAnnotationsExtensions
    {
        private static readonly object ComposerKey   = new();
        private static readonly object ValidationKey = new();

        public static void SetValidation(
            this ValidationContext context, Validation validation)
        {
            context.Items[ValidationKey] = validation;
        }

        public static Validation GetValidation(this ValidationContext context)
        {
            return context.Items.TryGetValue(ValidationKey, out var validation)
                 ? (Validation)validation
                 : null;
        }

        public static void SetComposer(this ValidationContext context, IHandler composer)
        {
            context.Items[ComposerKey] = composer;
        }

        public static IHandler GetComposer(this ValidationContext context)
        {
            return context.Items.TryGetValue(ComposerKey, out var composer)
                 ? (IHandler)composer
                 : null;
        }
    }
}
