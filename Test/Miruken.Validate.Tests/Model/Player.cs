namespace Miruken.Validate.Tests.Model;

using System;
using System.ComponentModel.DataAnnotations;

public class Player : Person
{
    [Required]
    public DateTime? DOB { get; set; }
}