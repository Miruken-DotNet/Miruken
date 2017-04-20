namespace Miruken.Callback
{
    public delegate object ProceedDelegate(bool proceed = true);

    public interface ICallbackFilter
    {
        object Filter(object callback, IHandler composer, ProceedDelegate proceed);
    }
}
