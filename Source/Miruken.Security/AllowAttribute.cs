namespace Miruken.Security
{
    using System;

    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Property,
        Inherited = false)]
    public class AllowAttribute : Attribute
    {
    }
}
