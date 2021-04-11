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
            string propertyPath, IValidationContext context)
        {
            var ruleSets = rule.RuleSets;
            if (ruleSets == null || ruleSets.Length == 0)
                return _scope.Matches(Scopes.Default);
            var scopes = ruleSets.Select(
                scope => scope != "default" ? scope : Scopes.Default);
            return scopes.Any(scope => _scope.Matches(scope));
        }
    }
}
