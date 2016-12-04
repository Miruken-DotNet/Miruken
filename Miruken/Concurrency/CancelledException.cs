using System;

namespace Miruken.Concurrency
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
