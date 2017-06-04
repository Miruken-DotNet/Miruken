namespace Miruken.Validate.Tests.Model
{
    using System.ComponentModel.DataAnnotations;

    public class Coach : Person
    {
        [Required]
        public string License { get; set; }
    }
}