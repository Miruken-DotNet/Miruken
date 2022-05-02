﻿namespace Miruken.Concurrency;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure;

public static class TaskExtensions
{
    public static Task<T> Cast<T>(this Task task)
    {
        return Helper.Coerce<T>(task);
    }

    public static Task Coerce(this Task task, Type taskType)
    {
        if (taskType == null)
            throw new ArgumentNullException(nameof(taskType));

        if (taskType.IsInstanceOfType(task)) return task;

        if (!taskType.IsGenericType ||
            taskType.GetGenericTypeDefinition() != typeof(Task<>))
            throw new ArgumentException($"{taskType.FullName} is not a Task<>");

        var resultType = taskType.GetGenericArguments()[0];
        var cast       = CoerceTask.GetOrAdd(resultType, rt =>
            RuntimeHelper.CreateStaticFuncOneArg<Helper, Task, Task>("Coerce", rt)
        );

        return cast(task);
    }

    public static Promise<object> ToPromise(
        this Task task,
        ChildCancelMode mode = ChildCancelMode.All,
        CancellationToken cancellationToken = default)
    {
        return task.Cast<object>().ToPromise(mode, cancellationToken);
    }

    public static Promise<object> ToPromise(
        this Task task,
        CancellationTokenSource cancellationTokenSource,
        ChildCancelMode mode = ChildCancelMode.All)
    {
        return task.Cast<object>().ToPromise(cancellationTokenSource, mode);
    }

    public static Promise<T> ToPromise<T>(
        this Task<T> task,
        ChildCancelMode mode = ChildCancelMode.All,
        CancellationToken cancellationToken = default)
    {
        return new Promise<T>(task, cancellationToken, mode);
    }

    public static Promise<T> ToPromise<T>(
        this Task<T> task,
        CancellationTokenSource cancellationTokenSource,
        ChildCancelMode mode = ChildCancelMode.All)
    {
        return new Promise<T>(task, cancellationTokenSource, mode);
    }

    public static Exception FlattenException(this Task task)
    {
        var exception = task?.Exception?.Flatten();
        return exception?.InnerExceptions.Count == 1
            ? exception.InnerException
            : exception;
    }

    private static readonly ConcurrentDictionary<Type, Func<Task, Task>>
        CoerceTask = new();

    #region Helper

    private class Helper
    {
        internal static Task<T> Coerce<T>(Task task)
        {
            if (task is Task<T> t) return t;
            return task.IsCompleted 
                ? Complete<T>(task) 
                : task.ContinueWith(Complete<T>,
                        TaskContinuationOptions.ExecuteSynchronously)
                    .Unwrap();
        }

        private static Task<T> Complete<T>(Task task)
        {
            var source = new TaskCompletionSource<T>();
            if (task.IsFaulted)
                source.SetException(task.FlattenException());
            else if (task.IsCanceled)
                source.SetCanceled();
            else
                source.SetResult((T)GetResult(task, typeof(T)));
            return source.Task;
        }

        private static object GetResult(Task task, Type type = null)
        {
            var taskType = task.GetType();
            var getter = TaskResultGetters.GetOrAdd(taskType, t =>
                t.GetOpenTypeConformance(typeof(Task<>)) == null ? null
                    : RuntimeHelper.CreatePropertyGetter("Result", t));
            return getter?.Invoke(task) ?? RuntimeHelper.GetDefault(type);
        }

        private static readonly ConcurrentDictionary<Type, Func<object, object>>
            TaskResultGetters = new();
    }

    #endregion
}