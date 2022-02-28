namespace Miruken.Api;

using System;
using Callback;

public abstract class StashAction : IFilterCallback
{
    bool IFilterCallback.CanFilter => false;

    public sealed class Get : StashAction
    {
        public Type   Type  { get; }
        public object Value { get; set; }

        public Get(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }

    public sealed class Put : StashAction
    {
        public Type   Type  { get; }
        public object Value { get; }

        public Put(Type type, object value)
        {
            Type  = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
        }
    }

    public sealed class Drop : StashAction
    {
        public Type Type { get; }

        public Drop(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}