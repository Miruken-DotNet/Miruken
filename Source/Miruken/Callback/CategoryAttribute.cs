namespace Miruken.Callback
{
    using System;
    using Policy;
    using Policy.Bindings;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public abstract class CategoryAttribute : Attribute
    {
        public object InKey  { get; protected set; }
        public object OutKey { get; protected set; }
        public bool   Strict { get; set; }

        public abstract CallbackPolicy CallbackPolicy { get; }

        public virtual bool Approve(object callback, PolicyMemberBinding binding)
        {
            return true;
        }

        protected const string _ = null;
    }
}