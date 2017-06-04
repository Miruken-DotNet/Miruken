using System;

namespace Miruken.Infrastructure
{
    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception exeception)
        {
            Exeception = exeception;
        }

        public Exception Exeception { get; private set; }
    }
}
