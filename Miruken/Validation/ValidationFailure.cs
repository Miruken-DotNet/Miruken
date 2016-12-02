namespace Miruken.Validation
{
    public class ValidationFailure
    {
        public string ErrorCode      { get; set; }

        public string PropertyName   { get; set; }

        public string ErrorMessage   { get; set; }

        public object AttemptedValue { get; set; }
    }
}
