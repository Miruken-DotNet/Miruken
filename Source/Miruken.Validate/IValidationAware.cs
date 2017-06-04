namespace Miruken.Validate
{
    public interface IValidationAware
    {
        ValidationOutcome ValidationOutcome { get; set; }
    }
}
