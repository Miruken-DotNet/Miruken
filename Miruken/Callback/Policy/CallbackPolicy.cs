namespace Miruken.Callback.Policy
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public abstract class CallbackPolicy
    {
        private readonly List<MethodRule> _rules = new List<MethodRule>();

        public void AddMethodRule(MethodRule rule)
        {
            _rules.Add(rule);
        }

        public MethodRule MatchMethod(MethodInfo method, DefinitionAttribute attribute)
        {
            return _rules.FirstOrDefault(r => r.Matches(method, attribute));
        }

        public abstract IEnumerable SelectKeys(object callback, ICollection keys);
    }
}