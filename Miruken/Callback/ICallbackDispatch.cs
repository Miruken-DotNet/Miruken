namespace Miruken.Callback
{
    public interface ICallbackDispatch
    {
        bool Dispatch(Handler handler, bool greedy, IHandler composer);
    }
}
