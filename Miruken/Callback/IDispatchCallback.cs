namespace Miruken.Callback
{
    public interface IDispatchCallback
    {
        bool Dispatch(Handler handler, bool greedy, IHandler composer);
    }
}
