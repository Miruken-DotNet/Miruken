namespace Miruken.Validate
{
    using Concurrency;

    public interface IValidating
    {
        ValidationOutcome Validate(object target, params object[] scopes);

        Promise<ValidationOutcome> ValidateAsync(object target, params object[] scopes);
    }

    public interface IValidator : IValidating
    {
    }
}
