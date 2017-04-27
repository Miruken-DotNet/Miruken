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

        public ComposerArgument Composer => ComposerArgument.Instance;

        public static ReturnArgConstraint Arg(int argIndex)
        {
            if (argIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(argIndex),
                    "Argument index must be >= 1");
            return new ReturnArgConstraint(argIndex - 1);
        }

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
            Policy.AddMethodRule(new MethodRule(Policy.BindMethod, returnRule, args));
            return (TBuilder)this;
        }
    }
}