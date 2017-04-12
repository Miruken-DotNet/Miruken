namespace Miruken.Callback.Policy
{
    using System;

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public abstract class CallbackFilterAttribute : Attribute, ICallbackFilter
    {
        public abstract bool Accepts(object callback, IHandler composer);
    }
}
