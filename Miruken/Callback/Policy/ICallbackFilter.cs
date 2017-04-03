namespace Miruken.Callback.Policy
{
    public interface ICallbackFilter
    {
        bool Accepts(object callback, IHandler composer);
    }
}
