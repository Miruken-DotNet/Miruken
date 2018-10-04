namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Collections.Generic;

    public class Metadata : IBindingConstraint
    {
        private readonly IDictionary<object, object> _metadata;

        public Metadata(IDictionary<object, object> metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            _metadata = new Dictionary<object, object>(metadata);
        }

        public void Require(BindingMetadata metadata)
        {
            foreach (var item in _metadata)
                metadata.Set(item.Key, item.Value);
        }

        public bool Matches(BindingMetadata metadata)
        {
            foreach (var item in _metadata)
            {
                if (!(metadata.Get(item.Key, out object value) &&
                      Equals(item.Value, value))) return false;
            }
            return true;
        }
    }

    public class MetadataKey : IBindingConstraint
    {
        private readonly object _key;
        private readonly object _value;

        public MetadataKey(object key, object value)
        {
            _key   = key ?? throw new ArgumentNullException(nameof(key));
            _value = value;
        }

        public void Require(BindingMetadata metadata)
        {
            metadata.Set(_key, _value);
        }

        public bool Matches(BindingMetadata metadata)
        {
            return metadata.Get(_key, out object value)
                   && Equals(_value, value);
        }
    }
}
