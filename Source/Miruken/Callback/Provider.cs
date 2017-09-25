namespace Miruken.Callback
{
    using System;
    using Infrastructure;

    public class Provider : Handler
    {
        private readonly object _value;
        private readonly Type _providesType;

        public Provider(object value)
        {
            _value = value;
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            _providesType = value.GetType();
            if (_providesType.IsArray)
                _providesType = _providesType.GetElementType();
        }

        [Provides]
        public object Provide(Inquiry inquiry)
        {
            var type = inquiry.Key as Type;
            if (type == null) return null;
            return _providesType.Is(type) ? _value : null;
        }
    }
}
