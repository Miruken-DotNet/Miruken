namespace Miruken.Callback
{
    public interface ICallbackFilter
    {
        bool Accepts(object callback, IHandler composer);
    }
}
