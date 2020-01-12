namespace Miruken.Validate
{
    using Callback;

    public class ValidateAttribute : FilterAttribute
    {
        public ValidateAttribute() : base(typeof(ValidateFilter<,>))
        {         
        }

        public bool ValidateResult { get; set; }
    }
}
