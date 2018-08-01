using Miruken.Concurrency;

namespace Miruken.Callback
{
    public class RejectedException : CancelledException
    {
        public RejectedException()
        {       
        }

        public RejectedException(string message)
            : base(message)
        {          
        }

        public RejectedException(object callback)
            : base("Callback has been cancelled")
        {
            Callback = callback;
        }

        public object Callback { get; }
    }
}
