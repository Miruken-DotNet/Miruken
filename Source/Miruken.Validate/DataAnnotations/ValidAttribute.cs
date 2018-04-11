namespace Miruken.Validate.DataAnnotations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Callback;
    using Infrastructure;

    [AttributeUsage(AttributeTargets.Property | 
                    AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ValidAttribute : ValidationAttribute
    {
        private readonly Type[] _validators;

        public ValidAttribute()
        {           
        }

        public ValidAttribute(params Type[] validators)
        {
            if (validators.Length > 0 && validators.Any(v => !v.Is<ValidationAttribute>()))
                throw new ArgumentException(
                    @"All validators must extend ValidationAttribute", nameof(validators));
            _validators = validators;
        }

        public object Scope { get; set; }

        protected override ValidationResult IsValid(
            object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var memberName = validationContext.MemberName;
            var composer   = validationContext.GetComposer();

            if (_validators?.Length > 0 || value.GetType().IsSimpleType())
                return ValidateValue(value, memberName, composer);

            var scopes = GetScopes(validationContext);
            return ValidateCompose(value, memberName, composer, scopes);
        }

        protected ValidationResult ValidateValue(
            object value, string memberName, IServiceProvider composer)
        {
            var results    = new List<ValidationResult>();
            var context    = new ValidationContext(value, composer, null);
            var validators = _validators.Select(Activator.CreateInstance)
                .Cast<ValidationAttribute>();
            return Validator.TryValidateValue(value, context, results, validators)
                 ? ValidationResult.Success
                 : new CompositeResult(memberName, results);
        }

        protected ValidationResult ValidateCompose(
            object value, string memberName, IHandler composer, 
            params object[] scopes)
        {
            var outcome = composer.Proxy<IValidating>().Validate(value, scopes);
            var validationAware = value as IValidationAware;
            if (validationAware != null)
                validationAware.ValidationOutcome = outcome;
            return outcome.IsValid
                 ? ValidationResult.Success
                 : new CompositeResult(memberName, outcome);
        }

        protected object[] GetScopes(ValidationContext validationContext)
        {
            var scopes = Scope as object[];
            if (scopes != null) return scopes;
            var scope = Scope ?? validationContext.GetValidation()?.ScopeMatcher;
            return scope != null ? new[] { scope } : Array.Empty<object>();
        }
    }

    public class ValidCollectionAttribute : ValidAttribute
    {
        public ValidCollectionAttribute()
        {
        }

        public ValidCollectionAttribute(params Type[] validators)
            : base(validators)
        {
        }

        protected override ValidationResult IsValid(
            object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var enumerable = value as IEnumerable;
            if (enumerable == null)
                throw new ArgumentException("Target is not a collection");

            var memberName = validationContext.MemberName;
            var composer   = validationContext.GetComposer();
            var scopes     = GetScopes(validationContext);

            var results = enumerable.Cast<object>()
                .Select((v, i) => v == null || v.GetType().IsSimpleType()
                    ? ValidateValue(v, i.ToString(), composer)
                    : ValidateCompose(v, i.ToString(), composer, scopes))
                    .Where(result => result != ValidationResult.Success);

            return new CompositeResult(memberName, results);
        }
    }
}
