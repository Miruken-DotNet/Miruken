namespace Miruken.Callback.Policy
{
    public interface ICallbackPolicyDispatch
    {
        bool Dispatch(
            CallbackPolicy policy, object callback, bool greedy,
            IHandler composer, ResultsDelegate results = null);
    }
}
