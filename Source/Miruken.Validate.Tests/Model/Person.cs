namespace Miruken.Validate.Tests.Model
{
    using System.ComponentModel.DataAnnotations;

    public class Person : Model
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName  { get; set; }
    }
}