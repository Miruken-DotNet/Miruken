namespace Miruken.Callback;

using System;

public class NotHandledException : Exception
{
    public NotHandledException(object callback)
    {
        Callback = callback;
    }

    public NotHandledException(object callback, string message)
        : base(message)
    {
        Callback = callback;
    }

    public object Callback { get; }
}