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

        public MethodDefinition Accept(MethodInfo method)
        {
            var definition = Match(method);
            if (definition != null) return definition;
            throw new InvalidOperationException(
                $"Policy for {GetType().FullName} rejected method {GetDescription(method)}");
        }

        public abstract MethodDefinition Match(MethodInfo method);

        public virtual bool Validate(
            object callback, IHandler composer, Func<object> dispatch)
        {
            dispatch();
            return true;
        }

        protected string GetDescription(MethodInfo method)
        {
            return $"{method.ReflectedType?.FullName}:{method.Name}";
        }
    }
}