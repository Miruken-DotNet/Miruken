namespace Miruken.Validate.FluentValidation
{
    using System.Threading;
    using System.Threading.Tasks;
    using global::FluentValidation;
    using global::FluentValidation.Results;

    internal class FluentValidator<T> : AbstractValidator<T>
    {
        public static readonly FluentValidator<T> Instance = new FluentValidator<T>();

        private FluentValidator()
        {      
        }

        public override ValidationResult Validate(ValidationContext<T> context)
        {
            var composer = context.GetComposer();
            if (composer == null) return new ValidationResult();
            var target  = context.InstanceToValidate;
            var scope   = context.GetValidation()?.ScopeMatcher;   
            var outcome = composer.Proxy<IValidating>().Validate(target, scope);
            return CreateResult(outcome, context);
        }

        public override async Task<ValidationResult> ValidateAsync(
            ValidationContext<T> context, CancellationToken cancellation = new CancellationToken())
        {
            var composer = context.GetComposer();
            if (composer == null) return new ValidationResult();
            var target  = context.InstanceToValidate;
            var scope   = context.GetValidation()?.ScopeMatcher;
            var outcome = await composer.Proxy<IValidating>().ValidateAsync(target, scope);
            return CreateResult(outcome, context);
        }

        private static ValidationResult CreateResult(
             ValidationOutcome outcome, ValidationContext context)
        {
            var validationAware = context.InstanceToValidate as IValidationAware;
            if (validationAware != null)
                validationAware.ValidationOutcome = outcome;
            return outcome.IsValid ? new ValidationResult()
                 : new ValidationResult(new[]
                 {
                     new OutcomeFailure(context.PropertyChain.ToString(), outcome)
                 });
        }
    }
}
