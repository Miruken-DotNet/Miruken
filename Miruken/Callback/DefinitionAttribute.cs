namespace Miruken.Callback
{
    using System;
    using System.Reflection;
    using Policy;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public abstract class DefinitionAttribute : Attribute
    {
        public object Key       { get; set; }
        public bool   Invariant { get; set; }

        public abstract CallbackPolicy MethodPolicy { get; }

        public abstract MethodDefinition MatchMethod(MethodInfo method);  
    }
}