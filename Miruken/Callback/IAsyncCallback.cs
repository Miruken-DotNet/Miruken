namespace Miruken.Callback
{
    public interface IAsyncCallback
    {
        bool IsAsync    { get; }
        bool WantsAsync { get; }
    }
}
