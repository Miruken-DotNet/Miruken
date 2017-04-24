namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public delegate MethodBinding BindMethodDelegate(
        MethodRule rule, MethodDispatch dispatch,
        DefinitionAttribute attribute, CallbackPolicy policy);

    public class MethodBinding
    {
        private Type _varianceType;
        private List<IFilterProvider> _filters;
        private MethodPipeline _pipeline;
        private bool _initialized;
        private object _lock;

        public MethodBinding(MethodRule rule,
                             MethodDispatch dispatch,
                             DefinitionAttribute attribute,
                             CallbackPolicy policy)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            Rule       = rule;
            Dispatcher = dispatch;
            Attribute  = attribute;
            Policy     = policy;
            AddFilters(FilterAttribute.GetFilters(Dispatcher.Method));
        }

        public MethodRule          Rule       { get; }
        public DefinitionAttribute Attribute  { get; }
        public MethodDispatch      Dispatcher { get; }
        public CallbackPolicy      Policy     { get; }

        public Type VarianceType
        {
            get { return _varianceType; }
            set
            {
                if (value?.ContainsGenericParameters == true &&
                    !value.IsGenericTypeDefinition)
                    _varianceType = value.GetGenericTypeDefinition();
                else
                    _varianceType = value;
            }
        }

        public object GetKey()
        {
            var key = Attribute.Key;
            return key == null || key is Type ? VarianceType : key;
        }

        public void AddFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            if (_filters == null)
                _filters = new List<IFilterProvider>();
            _filters.AddRange(providers.Where(p => p != null));
        }

        public virtual bool Dispatch(object target, object callback, 
            IHandler composer, IEnumerable<IFilterProvider> providers)
        {
            var resultType = Policy.ResultType?.Invoke(callback);
            var result     = Invoke(target, callback, composer,providers, resultType);
            return Policy.HasResult?.Invoke(result) ?? result != null;
        }

        protected object Invoke(object target, object callback,
            IHandler composer, IEnumerable<IFilterProvider> providers,
            Type returnType = null)
        {
            var args = Rule.ResolveArgs(this, callback, composer);

            if (callback is FilterOptions)
                return Dispatcher.Invoke(target, args, returnType);

            var options = composer.GetFilterOptions();
            if (options?.SuppressFilters == true)
                return Dispatcher.Invoke(target, args, returnType);

            var callbackType = callback.GetType();
            var resultType   = Dispatcher.ReturnType;
            if (resultType == typeof(void))
                resultType = typeof(object);
            var filters = composer
                .ResolveOpenFilters(callbackType, resultType)
                .GetOrderedFilters(options, _filters, providers, Policy.Filters)
                .ToArray();

            if (filters.Length == 0)
                return Dispatcher.Invoke(target, args, returnType);

            var pipeline = LazyInitializer.EnsureInitialized(
                ref _pipeline, ref _initialized, ref _lock, () =>
                {
                    var pipelineType = typeof(MethodPipeline<,>)
                        .MakeGenericType(callbackType, resultType);
                    return (MethodPipeline)Activator.CreateInstance(pipelineType);
                });

            object result;
            return pipeline.Invoke(this, target, callback, args,
                returnType, composer, filters, out result)
                ? result : Policy.NoResult;
        }
    }
}
