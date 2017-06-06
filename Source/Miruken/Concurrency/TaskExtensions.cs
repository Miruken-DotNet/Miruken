namespace Miruken.Concurrency
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure;

    public static class TaskExtensions
    {
        internal class Helper
        {
            internal static Task<T> Coerce<T>(Task task)
            {
                return task as Task<T> ??
                    task.ContinueWith(t => (T)GetResult(t, typeof(T)));
            }
        }

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
             CancellationToken cancellationToken = default(CancellationToken),
             ChildCancelMode mode = ChildCancelMode.All)
        {
            return task.ContinueWith(t => GetResult(t), cancellationToken)
                .ToPromise(cancellationToken, mode);
        }

        public static Promise<object> ToPromise(
            this Task task,
            CancellationTokenSource cancellationTokenSource,
            ChildCancelMode mode = ChildCancelMode.All)
        {
            return task.ContinueWith(t => GetResult(t), cancellationTokenSource.Token)
                .ToPromise(cancellationTokenSource, mode);
        }

        public static Promise<T> ToPromise<T>(
            this Task<T> task,
            CancellationToken cancellationToken = default(CancellationToken),
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

        private static object GetResult(Task task, Type type = null)
        {
            var exception = task.Exception;
            if (exception != null)
            {
                var ex = exception.Flatten().InnerException;
                ExceptionDispatchInfo.Capture(ex ?? exception).Throw();
            }
            var taskType = task.GetType();
            var getter   = TaskResultGetters.GetOrAdd(taskType, ttype =>
              ttype.GetOpenTypeConformance(typeof(Task<>)) == null ? null
                  : RuntimeHelper.CreatePropertyGetter("Result", ttype));
            return getter?.Invoke(task) ?? RuntimeHelper.GetDefault(type);
        }

        private static readonly ConcurrentDictionary<Type, PropertyGetDelegate>
            TaskResultGetters = new ConcurrentDictionary<Type, PropertyGetDelegate>();

        private static readonly ConcurrentDictionary<Type, Func<Task, Task>>
            CoerceTask = new ConcurrentDictionary<Type, Func<Task, Task>>();
    }
}
