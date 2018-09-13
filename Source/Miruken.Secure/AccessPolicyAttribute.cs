namespace Miruken.Secure
{
    using System;

    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Property,
        Inherited = false)]
    public class AccessPolicyAttribute : Attribute
    {
        public AccessPolicyAttribute(string policy)
        {
            Policy = policy;
        }

        public string Policy { get; }
    }
}
