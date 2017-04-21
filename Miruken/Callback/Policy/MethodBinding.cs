namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MethodBinding
    {
        private Type _varianceType;
        private List<CallbackFilterAttribute> _filters;

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

            var pipeline = GetPipeline(composer).GetEnumerator();

            CallbackFilterDelegate next = null;
            next = proceed => {
                if (!proceed)
                {
                    pipeline.Dispose();
                    return NoResult;
                }
                if (pipeline.MoveNext())
                    return pipeline.Current.Filter(callback, composer, next);
                pipeline.Dispose();
                return Dispatcher.Invoke(target, args, returnType);
            };

            return next();
        }

        protected void AddFilters(params CallbackFilterAttribute[] filters)
        {
            if (filters == null || filters.Length == 0) return;
            if (_filters == null)
                _filters = new List<CallbackFilterAttribute>();
            _filters.AddRange(filters);
        }

        private IEnumerable<ICallbackFilter> GetPipeline(IHandler composer)
        {
            return _filters
                .OrderByDescending(filter => filter)
                .Select(filter => filter.FilterType != null
                                ? composer.Resolve(filter.FilterType)
                                : filter)
                .OfType<ICallbackFilter>();
        }

        private void AddMethodFilters()
        {
            AddFilters((CallbackFilterAttribute[])Dispatcher.Method
                .GetCustomAttributes(typeof(CallbackFilterAttribute), true));
        }
    }
}
