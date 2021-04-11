namespace Miruken.Validate.FluentValidation
{
    using System.Threading;
    using System.Threading.Tasks;
    using global::FluentValidation;
    using global::FluentValidation.Results;

    internal class FluentValidator<T> : AbstractValidator<T>
    {
        public static readonly FluentValidator<T> Instance = new();

        private FluentValidator()
        {
        }

        public override ValidationResult Validate(ValidationContext<T> context)
        {
            var composer = context.GetComposer();
            if (composer == null) return new ValidationResult();
            var target  = context.InstanceToValidate;
            var scope   = context.GetValidation()?.ScopeMatcher;   
            var outcome = composer.Validate(target, scope);
            return CreateResult(outcome, context);
        }

        public override async Task<ValidationResult> ValidateAsync(
            ValidationContext<T> context, CancellationToken cancellation = new())
        {
            var composer = context.GetComposer();
            if (composer == null) return new ValidationResult();
            var target  = context.InstanceToValidate;
            var scope   = context.GetValidation()?.ScopeMatcher;
            var outcome = await composer.ValidateAsync(target, scope)
                .ConfigureAwait(false);
            return CreateResult(outcome, context);
        }

        private static ValidationResult CreateResult(
             ValidationOutcome outcome, ValidationContext<T> context)
        {
            if (context.InstanceToValidate is IValidationAware validationAware)
                validationAware.ValidationOutcome = outcome;
            if (outcome.IsValid) return new ValidationResult();
            
            var failure = new ValidationFailure(
                context.PropertyChain.ToString(),
                outcome.Error)
            {
                CustomState = outcome
            };
            context.AddFailure(failure);
            
            return  new ValidationResult(new[] { failure });
        }
    }
}
