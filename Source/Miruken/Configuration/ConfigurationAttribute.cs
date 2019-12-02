#if NETSTANDARD
namespace Miruken.Configuration
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigurationAttribute : Attribute
    {
        public ConfigurationAttribute()
        {
        }

        public ConfigurationAttribute(string key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public string Key { get; }
    }
}
#endif
