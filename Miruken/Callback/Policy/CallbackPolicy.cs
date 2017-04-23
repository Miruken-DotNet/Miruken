namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public abstract class CallbackPolicy
    {
        private readonly List<MethodRule> _rules = new List<MethodRule>();
        private List<IFilterProvider> _filters;

        private static readonly ConcurrentDictionary<Type, HandlerDescriptor>
            _descriptors = new ConcurrentDictionary<Type, HandlerDescriptor>();

        public object             NoResult   { get; set; }
        public Func<object, bool> HasResult  { get; set; }
        public Func<object, Type> ResultType { get; set; }
        public BindMethodDelegate Binder     { get; set; }

        public IEnumerable<IFilterProvider> Filters =>
            _filters ?? Enumerable.Empty<IFilterProvider>();

        public void AddMethodRule(MethodRule rule)
        {
            _rules.Add(rule);
        }

        public MethodRule MatchMethod(MethodInfo method, DefinitionAttribute attribute)
        {
            return _rules.FirstOrDefault(r => r.Matches(method, attribute));
        }

        public virtual MethodBinding BindMethod(MethodRule rule,
            MethodDispatch dispatch, DefinitionAttribute attribute)
        {
            return Binder?.Invoke(rule, dispatch, attribute, this)
                ?? new MethodBinding(rule, dispatch, attribute, this);
        }

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            if (_filters == null)
                _filters = new List<IFilterProvider>();
            _filters.AddRange(providers.Where(p => p != null));
        }

        public abstract IEnumerable SelectKeys(object callback, ICollection keys);

        public bool Dispatch(
            Handler handler, object callback, bool greedy, IHandler composer)
        {
            var handled   = false;
            var surrogate = handler.Surrogate;

            if (surrogate != null)
            {
                var descriptor = GetDescriptor(surrogate.GetType());
                handled = descriptor.Dispatch(this, surrogate, callback, greedy, composer);
            }

            if (!handled || greedy)
            {
                var descriptor = GetDescriptor(handler.GetType());
                handled = descriptor.Dispatch(this, handler, callback, greedy, composer)
                       || handled;
            }

            return handled;
        }

        public static HandlerDescriptor GetDescriptor(Type type)
        {
            return _descriptors.GetOrAdd(type, t => new HandlerDescriptor(t));
        }
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