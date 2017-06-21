namespace Miruken.Callback
{
    using System;

    public interface ICallback
    {
        Type   ResultType { get; }
        object Result     { get; set; }
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

    public interface IInvokeCallback {}

    public interface IDispatchCallback
    {
        bool Dispatch(object handler, ref bool greedy, IHandler composer);
    }
}