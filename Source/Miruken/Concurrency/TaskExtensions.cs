namespace Miruken.Concurrency
{
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
             CancellationToken cancellationToken = default,
             ChildCancelMode mode = ChildCancelMode.All)
        {
            return task.Cast<object>().ToPromise(cancellationToken, mode);
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
            CancellationToken cancellationToken = default,
            ChildCancelMode mode = ChildCancelMode.All)
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
            CoerceTask = new ConcurrentDictionary<Type, Func<Task, Task>>();

        #region Helper

        internal class Helper
        {
            internal static Task<T> Coerce<T>(Task task)
            {
                if (task is Task<T> ttask) return ttask;
                return task.IsCompleted 
                     ? Complete<T>(task) 
                     : task.ContinueWith(Complete<T>).Unwrap();
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
                var getter = TaskResultGetters.GetOrAdd(taskType, ttype =>
                ttype.GetOpenTypeConformance(typeof(Task<>)) == null ? null
                    : RuntimeHelper.CreatePropertyGetter("Result", ttype));
                return getter?.Invoke(task) ?? RuntimeHelper.GetDefault(type);
            }

            private static readonly ConcurrentDictionary<Type, Func<object, object>>
                TaskResultGetters = new ConcurrentDictionary<Type, Func<object, object>>();
        }

        #endregion
    }
}
