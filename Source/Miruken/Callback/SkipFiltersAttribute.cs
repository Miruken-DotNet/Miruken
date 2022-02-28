namespace Miruken.Callback;

using System;

[AttributeUsage(AttributeTargets.Class  |
                AttributeTargets.Method |
                AttributeTargets.Property,
    Inherited = false)]
public sealed class SkipFiltersAttribute : Attribute
{
}