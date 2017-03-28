namespace Miruken.Callback.Policy
{
    using System;

    public class CallbackTypeFilter : ICallbackFilter
    {
        private readonly Type _constraint;
        private readonly Type _genericConstraint;
        private readonly Func<object, object> _extract;

        public CallbackTypeFilter(Type constraint, Func<object, object> extract = null)
        {
            if (constraint == null)
                throw new ArgumentNullException(nameof(constraint));
            _constraint = constraint;
            _extract    = extract;
            if (_constraint.IsGenericType && _constraint.ContainsGenericParameters)
                _genericConstraint = _constraint.GetGenericTypeDefinition();
        }

        public bool Invariant { get; set; }

        public bool Accepts(DefinitionAttribute definition, object callback, IHandler composer)
        {
            if (callback == null) return false;
            if (_extract != null)
                callback = _extract(callback);
            if (callback == null) return false;
            var callbackType = callback.GetType();
            if ((Invariant && (_constraint == callbackType)) ||
                   _constraint.IsInstanceOfType(callback))
                return true;
            return _genericConstraint != null && callbackType.IsGenericType &&
                   _genericConstraint == callbackType.GetGenericTypeDefinition();
        }
    }
}