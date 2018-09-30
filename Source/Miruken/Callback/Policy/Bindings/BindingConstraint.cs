namespace Miruken.Callback.Policy.Bindings
{
    using System;

    public class BindingConstraint : IBindingConstraint
    {
        private readonly object _key;
        private readonly object _value;

        public BindingConstraint(object key, object value)
        {
            _key   = key;
            _value = value;
        }

        public void Require(BindingMetadata metadata)
        {
            metadata.Set(_key, _value);
        }

        public bool Matches(BindingMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            return metadata.Get<object>(_key, out var value)
                   && _value == value;
        }
    }
}
