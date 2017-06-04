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

    public interface INoFiltersCallback {}

    public interface IDispatchCallback
    {
        bool Dispatch(Handler handler, bool greedy, IHandler composer);
    }
}