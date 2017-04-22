namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public class MethodBinding
    {
        private Type _varianceType;
        private List<CallbackFilterAttribute> _filters;
        private MethodPipeline _pipeline;
        private bool _initialized;
        private object _lock;

        public MethodBinding(MethodRule rule,
                             MethodDispatch dispatch,
                             DefinitionAttribute attribute)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            Rule       = rule;
            Attribute  = attribute;
            Dispatcher = dispatch;
            AddMethodFilters();
        }

        public MethodRule          Rule       { get; }
        public DefinitionAttribute Attribute  { get; }
        public MethodDispatch      Dispatcher { get; }

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

        protected virtual object NoResult => null;

        public virtual object GetKey()
        {
            var key = Attribute.Key;
            return key == null || key is Type ? VarianceType : key;
        }

        public virtual bool Dispatch(object target, object callback, IHandler composer)
        {
            Invoke(target, callback, composer);
            return true;
        }

        protected object Invoke(object target, object callback,
                                IHandler composer, Type returnType = null)
        {
            var args = Rule.ResolveArgs(this, callback, composer);
            if (_filters == null)
                return Dispatcher.Invoke(target, args, returnType);

            var pipeline = LazyInitializer.EnsureInitialized(
                ref _pipeline, ref _initialized, ref _lock, () =>
                {
                    var resultType = Dispatcher.ReturnType;
                    if (resultType == typeof(void))
                        resultType = typeof(object);
                    var pipelineType = typeof(MethodPipeline<,>).MakeGenericType(
                        callback.GetType(), resultType);
                    return (MethodPipeline)Activator.CreateInstance(pipelineType);
                });

            bool handled;
            var result = pipeline.Invoke(this, target, callback, args,
                returnType, _filters, composer, out handled);
            return handled ? result : NoResult;
        }

        protected void AddCallbackFilters(params CallbackFilterAttribute[] filters)
        {
            if (filters == null || filters.Length == 0) return;
            if (_filters == null)
                _filters = new List<CallbackFilterAttribute>();
            _filters.AddRange(filters.Where(f => f != null));
        }

        private void AddMethodFilters()
        {
            AddCallbackFilters((CallbackFilterAttribute[])Dispatcher.Method
                .GetCustomAttributes(typeof(CallbackFilterAttribute), true));
        }
    }
}
