using System;
using System.Threading;

namespace Miruken.Concurrency;

public abstract class AbstractAsyncResult : IAsyncResult
{
    protected object _result;
    protected Exception _exception;
    private readonly AsyncCallback _callback;
    private ManualResetEvent _waitEvent;
    private int _completed;

    protected AbstractAsyncResult()
    {
    }

    protected AbstractAsyncResult(AsyncCallback callback, object state)
    {
        AsyncState = state;
        _callback  = callback;
    }

    public object AsyncState { get; protected set; }

    public bool IsCompleted => _completed != 0;

    public bool CompletedSynchronously { get; protected set; }

    public WaitHandle AsyncWaitHandle
    {
        get
        {
            var isCompleted = _completed;

            if (_waitEvent == null)
            {
                Interlocked.CompareExchange(ref _waitEvent,
                    new ManualResetEvent(isCompleted != 0), null);
            }

            if ((isCompleted == 0) && (_completed != 0))
                _waitEvent.Set();

            return _waitEvent;
        }
    }

    public static object End(IAsyncResult asyncResult, int? millisecondsTimeout = null)
    {
        if (asyncResult == null)
            throw new ArgumentNullException(nameof(asyncResult));

        if (asyncResult is not AbstractAsyncResult result)
            throw new ArgumentException(@"Unrecognized IAsyncResult", nameof(asyncResult));

        if (result._completed == 0)
        {
            if (millisecondsTimeout.HasValue)
            {
                if (!result.AsyncWaitHandle.WaitOne(millisecondsTimeout.Value))
                    throw new TimeoutException();
            }
            else
            {
                result.AsyncWaitHandle.WaitOne();
            }
        }

        if (result._exception != null)
            throw result._exception;

        return result._result;
    }

    protected bool Complete(object result, bool synchronously, Action action = null)
    {
        if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0) 
            return false;
        _result = result;
        Complete(synchronously, action);
        return true;
    }

    protected bool Complete(Exception exception, bool synchronously, Action action = null)
    {
        if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0)
            return false;
        _exception = exception;
        Complete(synchronously, action);
        return true;
    }

    private void Complete(bool synchronously, Action action = null)
    {
        CompletedSynchronously = synchronously;
        action?.Invoke();
        _waitEvent?.Set();
        _callback?.Invoke(this);
    }
}