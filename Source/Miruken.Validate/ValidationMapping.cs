﻿namespace Miruken.Validate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Callback;
    using Map;

    public class ValidationErrors
    {
        public string             PropertyName { get; set; }
        public string[]           Errors       { get; set; }
        public ValidationErrors[] Nested       { get; set; }
    }

    public class ValidationMapping : Handler
    {
        [Maps, Format(typeof(Exception))]
        public ValidationErrors[] Map(ValidationException exception)
        {
            return CreateErrors(exception.Outcome);
        }

        [Maps, Format(typeof(Exception))]
        public ValidationException Map(ValidationErrors[] errors)
        {
            var outcome = CreateOutcome(errors);
            return new ValidationException(outcome);
        }

        private static ValidationOutcome CreateOutcome(ValidationErrors[] errors)
        {
            var outcome = new ValidationOutcome();
            if (errors == null) return outcome;
            foreach (var property in errors)
            {
                var propertyName = property.PropertyName;
                var failures     = property.Errors;
                if (failures != null)
                {
                    foreach (var error in failures)
                        outcome.AddError(propertyName, error);
                }
                var nested = property.Nested;
                if (nested != null && nested.Length > 0)
                    outcome.AddError(propertyName, CreateOutcome(nested));
            }
            return outcome;
        }

        private static ValidationErrors[] CreateErrors(ValidationOutcome outcome)
        {
            return outcome.Culprits.Select(culprit =>
            {
                var failure = new ValidationErrors
                {
                    PropertyName = culprit
                };
                var messages = new List<string>();
                var children = new List<ValidationErrors>();
                foreach (var error in outcome.GetErrors(culprit))
                {
                    var child = error as ValidationOutcome;
                    if (child != null)
                        children.AddRange(CreateErrors(child));
                    else
                        messages.Add(error.ToString());
                    if (messages.Count > 0)
                        failure.Errors = messages.ToArray();
                    if (children.Count > 0)
                        failure.Nested = children.ToArray();
                }
                return failure;
            }).ToArray();
        }
    }
}