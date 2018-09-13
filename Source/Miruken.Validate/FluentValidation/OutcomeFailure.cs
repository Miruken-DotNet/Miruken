namespace Miruken.Validate.FluentValidation
{
    using global::FluentValidation.Results;

    internal class OutcomeFailure : ValidationFailure
    {
        public OutcomeFailure(
            string propertyName, ValidationOutcome failedOutcome)
            : base(propertyName, "")
        {
            FailedOutcome = failedOutcome;
        }

        public ValidationOutcome FailedOutcome { get; }
    }
}
