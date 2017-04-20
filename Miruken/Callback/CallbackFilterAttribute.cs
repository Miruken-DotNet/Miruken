namespace Miruken.Callback
{
    using System;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter,
        AllowMultiple = true, Inherited = false)]
    public abstract class CallbackFilterAttribute : Attribute, ICallbackFilter
    {
        public abstract bool Accepts(object callback, IHandler composer);
    }
}
