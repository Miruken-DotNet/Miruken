namespace Miruken.Callback
{
    using System;
    using Policy;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public abstract class DefinitionAttribute : Attribute
    {
        public object Key { get; set; }

        public abstract CallbackPolicy CallbackPolicy { get; }

        protected const string _ = null;
    }
}