namespace Miruken.Validate.FluentValidation
{
    using System.Linq;
    using global::FluentValidation;
    using global::FluentValidation.Internal;

    public class ScopeSelector : IValidatorSelector
    {
        private readonly IScopeMatching _scope;

        public ScopeSelector(IScopeMatching scope)
        {
            _scope = scope;
        }

        public bool CanExecute(IValidationRule rule,
            string propertyPath, ValidationContext context)
        {
            var ruleSet = rule.RuleSet;
            if (string.IsNullOrEmpty(ruleSet))
                return _scope.Matches(Scopes.Default);
            var scopes = ruleSet.Split(',').Select(
                scope => scope != "default" ? scope : Scopes.Default);
            return scopes.Any(scope => _scope.Matches(scope));
        }
    }
}
