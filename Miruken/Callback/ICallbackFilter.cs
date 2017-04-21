namespace Miruken.Callback
{
    public delegate object CallbackFilterDelegate(bool proceed = true);

    public interface ICallbackFilter
    {
        object Filter(object callback, IHandler composer, CallbackFilterDelegate proceed);
    }
}
