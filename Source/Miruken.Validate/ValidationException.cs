namespace Miruken.Validate;

using System;

public class ValidationException : Exception
{
    public ValidationException(ValidationOutcome outcome)
        : base(outcome.Error)
    {
        Outcome = outcome;
    }

    public ValidationException(string message, ValidationOutcome outcome)
        : base(message)
    {
        Outcome = outcome;
    }

    public ValidationOutcome Outcome { get; }
}