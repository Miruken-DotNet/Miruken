namespace Miruken.Callback
{
    public interface ICancelCallback
    {
        bool ShouldCancel { get; }

        void Cancel();
    }
}
