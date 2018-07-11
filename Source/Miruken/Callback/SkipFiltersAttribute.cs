namespace Miruken.Callback
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        Inherited = false)]
    public sealed class SkipFiltersAttribute : Attribute
    {
    }
}
