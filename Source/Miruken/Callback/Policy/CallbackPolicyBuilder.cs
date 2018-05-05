namespace Miruken.Callback.Policy
{
    using System;

    public class CallbackPolicyBuilder<TPolicy, TBuilder>
        where TBuilder : CallbackPolicyBuilder<TPolicy, TBuilder>
        where TPolicy : CallbackPolicy
    {
        public CallbackPolicyBuilder(TPolicy policy)
        {
            Policy = policy;
        }

        protected TPolicy Policy { get; }

        public TBuilder AcceptResult(AcceptResultDelegate value)
        {
            Policy.AcceptResult = value;
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

        public TBuilder Filters(params IFilter[] filters)
        {
            Policy.AddFilters(new FilterInstancesProvider(filters));
            return (TBuilder)this;
        }

        public TBuilder Pipeline(params IFilterProvider[] providers)
        {
            Policy.AddFilters(providers);
            return (TBuilder)this;
        }

        public TBuilder MatchMethod(params ArgumentRule[] args)
        {
            Policy.AddMethodRule(new MethodRule(Policy.BindMethod, args));
            return (TBuilder)this;
        }

        public TBuilder MatchMethod(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (returnRule == null)
                throw new ArgumentNullException(nameof(returnRule));
            if (returnRule.GetInnerRule<ReturnAsync>() == null)
                returnRule = new ReturnAsync(returnRule, false);
            Policy.AddMethodRule(new MethodRule(Policy.BindMethod, returnRule, args));
            return (TBuilder)this;
        }
    }
}