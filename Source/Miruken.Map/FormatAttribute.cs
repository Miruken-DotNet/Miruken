namespace Miruken.Map
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        Inherited = false)]
    public class AnyFormatAttribute : Attribute, IFormatMatching
    {
        public bool Matches(object format)
        {
            return true;
        }
    }
}
