namespace Miruken.Validate.Tests.Model;

using System.ComponentModel.DataAnnotations;
using DataAnnotations;

public class Team : Model
{
    public int      Id         { get; set; }

    public bool     Active     { get; set; }

    [Required]
    public string   Name       { get; set; }

    [RegularExpression(@"^[u|U]\d\d?$",
        ErrorMessage = "The Division must match U followed by age.")]
    public string   Division   { get; set; }

    [Required, Valid]
    public Coach    Coach      { get; set; }

    [ValidCollection]
    public Player[] Players    { get; set; }

    public bool     Registered { get; set; }
}