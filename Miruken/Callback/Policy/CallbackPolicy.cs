namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public abstract class CallbackPolicy
    {
        public abstract bool Accepts(object callback);
        public abstract Type GetVarianceType(object callback);
    }

    public abstract class CallbackPolicy<Attrib> : CallbackPolicy
        where Attrib : DefinitionAttribute
    {
        private readonly List<MethodRule<Attrib>> _rules
            = new List<MethodRule<Attrib>>();

        public void AddMethodRule(MethodRule<Attrib> rule)
        {
            _rules.Add(rule);
        }

        public MethodDefinition<Attrib> MatchMethod(MethodInfo method, Attrib attribute)
        {
            return Match(method, attribute, _rules);
        }

        protected abstract MethodDefinition<Attrib> Match(
            MethodInfo method, Attrib attribute, 
            IEnumerable<MethodRule<Attrib>> rules);
    }
}