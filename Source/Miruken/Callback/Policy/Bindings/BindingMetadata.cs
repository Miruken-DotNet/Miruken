namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Collections.Generic;

    public class BindingMetadata
    {
        private readonly Dictionary<object, object> 
            _values = new Dictionary<object, object>();

        public string Name { get; set; }

        public bool Has(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return _values.ContainsKey(key);
        }

        public bool Get<V>(object key, out V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (_values.TryGetValue(key, out var v))
            {
                value = (V)v;
                return true;
            }
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            value = default;
            return false;
        }

        public void Set<V>(object key, V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _values[key] = value;
        }
    }
}
