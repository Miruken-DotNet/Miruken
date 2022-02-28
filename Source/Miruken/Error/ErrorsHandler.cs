namespace Miruken.Error;

using System;
using Callback;
using Concurrency;

public class ErrorsHandler : Handler, IErrors
{
    public virtual Promise HandleException(
        Exception exception, object callback, object context)
    {
        Console.WriteLine(exception);
        return Promise.Rejected(new RejectedException(callback));
    }
}