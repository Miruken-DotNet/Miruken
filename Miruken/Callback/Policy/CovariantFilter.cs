namespace Miruken.Callback.Policy
{
    using System;

    public class CovariantFilter<Cb> : ICallbackFilter
    {
        private readonly Type _constraint;
        private readonly Type _genericConstraint;
        private readonly Func<Cb, object> _key;

        public CovariantFilter(Type constraint, Func<Cb, object> key)
        {
            if (constraint == null)
                throw new ArgumentNullException(nameof(constraint));
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            _constraint = constraint;
            if (_constraint.IsGenericType && _constraint.ContainsGenericParameters)
                _genericConstraint = _constraint.GetGenericTypeDefinition();
            _key = key;
        }

        public bool Accepts(object callback, IHandler composer)
        {
            var key = _key((Cb)callback) as Type;
            if (key == null) return false;
            if (_constraint == typeof(object)) return true;
            if (_constraint.IsGenericType && _constraint.ContainsGenericParameters)
            {
                return _genericConstraint != null && key.IsGenericType &&
                       _genericConstraint == key.GetGenericTypeDefinition();
            }
            return key.IsAssignableFrom(_constraint);
        }
    }
}
