﻿namespace Miruken.Concurrency
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
        public Promise(Task<T> task)
            : this((resolve, reject) =>
            {
                if (task.IsFaulted)
                    reject(ExtractException(task.Exception), true);
                else if (task.IsCompleted)
                    resolve(task.Result, true);
                else if (!task.IsCanceled)
                {
                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            reject(ExtractException(t.Exception), false);
                        else if (t.IsCompleted)
                            resolve(t.Result, false);
                    });
                }
            })
        {
            if (task.IsCanceled) Cancel();
        }

        public Promise(Task<T> task, CancellationToken cancellationToken)
            : this(task)
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
            return aggregateException.InnerExceptions.First();
        }
    }

    public static class TaskToPromiseExtensions
    {
        public static Promise ToPromise(this Task task)
        {
            return new Promise<object>(task.ContinueWith(async t =>
            {
                await t;
                return (object) null;
            }).Unwrap());
        }

        public static Promise<T> ToPromise<T>(this Task<T> task)
        {
            return new Promise<T>(task);
        }
    }
}
