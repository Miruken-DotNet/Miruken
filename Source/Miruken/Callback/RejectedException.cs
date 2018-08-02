using Miruken.Concurrency;

namespace Miruken.Callback
{
    using System;

    public class RejectedException : CancelledException
    {
        public RejectedException()
        {       
        }

        public RejectedException(string message)
            : base(message)
        {          
        }

        public RejectedException(string message, Exception inner)
            : base(message, inner)
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
