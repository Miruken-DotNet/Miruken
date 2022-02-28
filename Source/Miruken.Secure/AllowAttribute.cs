namespace Miruken.Secure;

using System;

[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Property,
    Inherited = false)]
public class AllowAttribute : Attribute
{
}