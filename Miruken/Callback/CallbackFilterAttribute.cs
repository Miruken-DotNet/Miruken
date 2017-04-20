namespace Miruken.Callback
{
    using System;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter,
        AllowMultiple = true, Inherited = false)]
    public abstract class CallbackFilterAttribute : Attribute, ICallbackFilter
    {
        public abstract object Filter(
            object callback, IHandler composer, ProceedDelegate proceed);
    }
}
