namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Collections.Generic;

    public class BindingMetadata
    {
        private readonly Dictionary<object, object> 
            _values = new();

        public string Name { get; set; }

        public bool IsEmpty => Name == null && _values.Count == 0;

        public bool Has(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return _values.ContainsKey(key);
        }

        public bool Get<TValue>(object key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (_values.TryGetValue(key, out var v))
            {
                value = (TValue)v;
                return true;
            }

            value = default;
            return false;
        }

        public void Set(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _values[key] = value;
        }

        public void MergeInto(BindingMetadata other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            foreach (var item in _values)
                other.Set(item.Key, item.Value);
        }
    }
}
