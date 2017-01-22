namespace Miruken.Concurrency
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class Promise
    {
        public Task<object> ToTask()
        {
            var tcs = new TaskCompletionSource<object>();
            Then((result, s) => tcs.SetResult(result))
            .Catch((exception, s) => tcs.SetException(exception))
            .Cancelled(cancel => tcs.SetCanceled());
            return tcs.Task;
        }

        public TaskAwaiter<object> GetAwaiter()
        {
            return ToTask().GetAwaiter();
        }

        public static implicit operator Task<object>(Promise promise)
        {
            return promise.ToTask();
        }
    }

    public partial class Promise<T>
    {
        public Promise(Task<T> task, ChildCancelMode mode = ChildCancelMode.All)
            : this(mode, (resolve, reject) =>
            {
                if (!task.IsCompleted)
                {
                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            reject(ExtractException(t.Exception), false);
                        else if (t.IsCanceled)
                            reject(new CancelledException("Task was cancelled"), false);
                        else
                            resolve(t.Result, false);
                    });
                }
                else if (task.IsFaulted)
                    reject(ExtractException(task.Exception), true);
                else if (task.IsCanceled)
                    reject(new CancelledException("Task was cancelled"), true);
                else
                    resolve(task.Result, true);
            })
        {
        }

        public Promise(Task<T> task, CancellationToken cancellationToken,
            ChildCancelMode mode = ChildCancelMode.All)
            : this(task, mode)
        {
            cancellationToken.Register(Cancel);
        }

        public new Task<T> ToTask()
        {
            var tcs = new TaskCompletionSource<T>();
            Then((result, s) => tcs.SetResult(result))
            .Catch((exception, s) => tcs.SetException(exception))
            .Cancelled(cancel => tcs.SetCanceled());
            return tcs.Task;
        }

        public new TaskAwaiter<T> GetAwaiter()
        {
            return ToTask().GetAwaiter();
        }

        public static implicit operator Task<T>(Promise<T> promise)
        {
            return promise.ToTask();
        }

        private static Exception ExtractException(AggregateException aggregateException)
        {
            var exceptions = aggregateException.InnerExceptions;
            return exceptions.Count == 1
                 ? aggregateException.InnerExceptions.First()
                 : aggregateException;
        }
    }

    public static class TaskToPromiseExtensions
    {
        public static Promise ToPromise(
            this Task task, ChildCancelMode mode = ChildCancelMode.All)
        {
            return task.ContinueWith(async t =>
            {
                await t;
                return (object) null;
            }).Unwrap().ToPromise(mode);
        }

        public static Promise ToPromise(
            this Task task, CancellationToken cancellationToken,
            ChildCancelMode mode = ChildCancelMode.All)
        {
            return task.ContinueWith(async t =>
            {
                await t;
                return (object) null;
            }, cancellationToken).Unwrap()
            .ToPromise(cancellationToken, mode);
        }
     
        public static Promise<T> ToPromise<T>(
            this Task<T> task, ChildCancelMode mode = ChildCancelMode.All)
        {
            return new Promise<T>(task, mode);
        }

        public static Promise<T> ToPromise<T>(
            this Task<T> task, CancellationToken cancellationToken,
            ChildCancelMode mode = ChildCancelMode.All)
        {
            return new Promise<T>(task, cancellationToken, mode);
        }
    }
}
