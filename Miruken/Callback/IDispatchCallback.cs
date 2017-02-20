namespace Miruken.Callback
{
    public interface IDispatchCallback : ICallback
    {
        bool Dispatch(Handler handler, bool greedy, IHandler composer);
    }
}
