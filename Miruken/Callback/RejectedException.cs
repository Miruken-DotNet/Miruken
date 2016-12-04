using SixFlags.CF.Miruken.Concurrency;

namespace SixFlags.CF.Miruken.Callback
{
    public class RejectedException : CancelledException
    {
        public RejectedException(object callback)
            : base("Callback has been cancelled")
        {
            Callback = callback;
        }

        public object Callback { get; private set; }
    }
}
