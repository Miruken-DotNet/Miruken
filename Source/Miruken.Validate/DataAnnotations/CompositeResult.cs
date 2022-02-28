namespace Miruken.Validate.DataAnnotations;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

internal class CompositeResult : ValidationResult
{
    public CompositeResult(
        string memberName, ValidationOutcome outcome)
        : base(outcome.Error, new[] { memberName })
    {
        Outcome = outcome;
    }

    public CompositeResult(
        string memberName, IEnumerable<ValidationResult> results)
        : base($"{memberName} failed validation.", new[] { memberName })
    {
        Outcome = CreateOutcome(results);
    }

    public ValidationOutcome Outcome { get; }

    private static ValidationOutcome CreateOutcome(
        IEnumerable<ValidationResult> results)
    {
        var outcome = new ValidationOutcome();
        foreach (var result in results)
        {
            if (result is not CompositeResult compositeResult)
            {
                var error = result.ErrorMessage;
                foreach (var memberName in result.MemberNames)
                    outcome.AddError(memberName, error);
            }
            else
            {
                foreach (var memberName in result.MemberNames)
                    outcome.AddError(memberName, compositeResult.Outcome);
            }
        }
        return outcome;
    }
}