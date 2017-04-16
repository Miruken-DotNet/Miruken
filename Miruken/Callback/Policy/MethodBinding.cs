namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MethodBinding
    {
        private Type _varianceType;
        private List<ICallbackFilter> _filters;

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
            var filter = attribute as ICallbackFilter;
            if (filter != null) AddFilters(filter);
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

        public virtual object GetKey()
        {
            var key = Attribute.Key;
            return key == null || key is Type ? VarianceType : key;
        }

        public bool Accepts(object callback, IHandler composer)
        {
            return _filters?.All(f => f.Accepts(callback, composer)) != false;
        }

        public virtual bool Dispatch(object target, object callback, IHandler composer)
        {
            return Accepts(callback, composer) && VerifyResult(target, callback, composer);
        }

        protected virtual bool VerifyResult(object target, object callback, IHandler composer)
        {
            Invoke(target, callback, composer);
            return true;
        }

        protected object Invoke(object target, object callback,
                                IHandler composer, Type returnType = null)
        {
            var args = Rule.ResolveArgs(this, callback, composer);
            return Dispatcher.Invoke(target, args, returnType);
        }

        internal void AddFilters(params ICallbackFilter[] filters)
        {
            if (filters == null || filters.Length == 0) return;
            if (_filters == null) _filters = new List<ICallbackFilter>();
            _filters.AddRange(filters);
        }
    }
}
