namespace Miruken.Callback
{
    using System;
    using Policy;

    public interface ICallback
    {
        Type   ResultType { get; }
        object Result     { get; set; }
    }

    public interface ICallbackKey
    {
        object Key { get; }
    }

    public interface IAsyncCallback
    {
        bool IsAsync    { get; }
        bool WantsAsync { get; }
    }

    public interface IBoundCallback
    {
        object Bounds { get; }
    }

    public interface IResolveCallback
    {
        object GetResolveCallback();
    }

    public interface IBatchCallback
    {
        bool CanBatch { get; }
    }

    public interface IFilterCallback
    {
        bool CanFilter { get; }
    }

    public interface IDispatchCallbackGuard
    {
        bool CanDispatch(object handler, PolicyMemberBinding binding);
    }

    public interface IDispatchCallback
    {
        CallbackPolicy Policy { get; }

        bool Dispatch(object handler, ref bool greedy, IHandler composer);
    }
}