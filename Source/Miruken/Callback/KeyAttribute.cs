namespace Miruken.Callback
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter)]
    public class KeyAttribute : Attribute
    {
        public KeyAttribute(object key)
        {
            Key = key;
        }

        public KeyAttribute(string key, StringComparison comparison)
        {
            Key = new StringKey(key, comparison);    
        }

        public object Key { get; }
    }
}
