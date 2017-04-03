namespace Miruken.Callback.Policy
{
    using System;

    public class ContravariantFilter : ICallbackFilter
    {
        private readonly Type _constraint;
        private readonly bool _invariant;
        private readonly Type _genericConstraint;

        public ContravariantFilter(Type constraint, bool invariant)
        {
            if (constraint == null)
                throw new ArgumentNullException(nameof(constraint));
            _constraint = constraint;
            _invariant = invariant;
            if (_constraint.IsGenericType && _constraint.ContainsGenericParameters)
                _genericConstraint = _constraint.GetGenericTypeDefinition();
        }

        public virtual bool Accepts(object callback, IHandler composer)
        {
            if (callback == null) return false;
            var callbackType = callback.GetType();
            if ((_invariant && (_constraint == callbackType)) ||
                _constraint.IsInstanceOfType(callback))
                return true;
            return _genericConstraint != null && callbackType.IsGenericType &&
                   _genericConstraint == callbackType.GetGenericTypeDefinition();
        }
    }

    public class ContravariantFilter<Cb> : ContravariantFilter
    {
        private readonly Func<Cb, object> _extract;

        public ContravariantFilter(Type constraint, bool invariant,
                                   Func<Cb, object> extract)
            : base(constraint, invariant)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            _extract = extract;
        }

        public override bool Accepts(object callback, IHandler composer)
        {
            if (!(callback is Cb)) return false;
            return base.Accepts(_extract((Cb)callback), composer);
        }
    }
}