namespace Miruken.Callback.Policy.Bindings;

using System;
using System.Collections.Generic;

public class MetadataConstraint : IBindingConstraint
{
    private readonly IDictionary<object, object> _metadata;

    public MetadataConstraint(IDictionary<object, object> metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));
        _metadata = new Dictionary<object, object>(metadata);
    }

    public void Require(BindingMetadata metadata)
    {
        foreach (var (key, value) in _metadata)
            metadata.Set(key, value);
    }

    public bool Matches(BindingMetadata metadata)
    {
        foreach (var (key, val) in _metadata)
        {
            if (!(metadata.Get(key, out object value) &&
                  Equals(val, value))) return false;
        }
        return true;
    }
}

public class MetadataKeyConstraint : IBindingConstraint
{
    private readonly object _key;
    private readonly object _value;

    public MetadataKeyConstraint(object key, object value)
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