namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public abstract class CallbackPolicy
    {
        private readonly List<MethodRule> _rules = new List<MethodRule>();

        public object             NoResult   { get; set; }
        public Func<object, bool> HasResult  { get; set; }
        public Func<object, Type> ResultType { get; set; }
        public BindMethodDelegate Binder     { get; set; }

        public void AddMethodRule(MethodRule rule)
        {
            _rules.Add(rule);
        }

        public MethodRule MatchMethod(MethodInfo method, DefinitionAttribute attribute)
        {
            return _rules.FirstOrDefault(r => r.Matches(method, attribute));
        }

        public MethodBinding BindMethod(MethodRule rule, MethodDispatch dispatch,
                                        DefinitionAttribute attribute)
        {
            return Binder?.Invoke(rule, dispatch, attribute, this)
                ?? new MethodBinding(rule, dispatch, attribute, this);
        }

        public abstract IEnumerable SelectKeys(object callback, ICollection keys);
    }

    public class CallbackPolicyBuilder<TPolicy, TBuilder>
        where TBuilder : CallbackPolicyBuilder<TPolicy, TBuilder>
        where TPolicy : CallbackPolicy
    {
        public CallbackPolicyBuilder(TPolicy policy)
        {
            Policy = policy;
        }

        protected TPolicy Policy { get; }

        public ComposerArgument Composer => ComposerArgument.Instance;

        public TBuilder NoResult(object value)
        {
            Policy.NoResult = value;
            return (TBuilder)this;
        }

        public TBuilder HasResult(Func<object, bool> value)
        {
            Policy.HasResult = value;
            return (TBuilder)this;
        }

        public TBuilder ResultType(Func<object, Type> value)
        {
            Policy.ResultType = value;
            return (TBuilder)this;
        }

        public TBuilder BindMethod(BindMethodDelegate binder)
        {
            Policy.Binder = binder;
            return (TBuilder)this;
        }
    }
}