namespace Miruken.Validate
{
    using Callback;

    public class ValidationOptions : Options<ValidationOptions>
    {
        public bool? StopOnFailure { get; set; }

        public override void MergeInto(ValidationOptions other)
        {
            if (StopOnFailure.HasValue && !other.StopOnFailure.HasValue)
                other.StopOnFailure = StopOnFailure;
        }
    }

    public static class ValidationOptionExtensions
    {
        public static IHandler StopOnFailure(
            this IHandler handler, bool? stopOnFailure = null)
        {
            return new ValidationOptions
            {
                StopOnFailure = stopOnFailure
            }.Decorate(handler);
        }
    }
}
