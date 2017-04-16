namespace Miruken.Callback
{
    using System;
    using System.Reflection;
    using Policy;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public abstract class DefinitionAttribute : Attribute
    {
        public object Key { get; set; }

        public abstract CallbackPolicy CallbackPolicy { get; }

        public MethodRule MatchMethod(MethodInfo method)
        {
            return CallbackPolicy.MatchMethod(method, this);
        }
    }
}