using System;

namespace SixFlags.CF.Miruken.Concurrency
{
    public class CancelledException : Exception
    {
        public CancelledException()
        {
        }

        public CancelledException(string message) : base(message)
        {          
        }
    }
}
