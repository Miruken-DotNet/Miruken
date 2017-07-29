namespace Miruken.Callback
{
    using System;
    using Policy;

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

    public interface IResolveCallback
    {
        object GetCallback(bool greedy);
    }

    public interface IInvokeOnlyCallback {}

    public interface IDispatchCallback
    {
        CallbackPolicy Policy { get; }

        bool Dispatch(object handler, ref bool greedy, IHandler composer);
    }
}