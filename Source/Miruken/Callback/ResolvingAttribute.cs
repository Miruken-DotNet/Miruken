namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;
    using Policy;

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ResolvingAttribute : Attribute, IArgumentResolver
    {
        public static readonly ResolvingAttribute 
            Default = new ResolvingAttribute();

        private static readonly MethodInfo CreateLazy =
            typeof(ResolvingAttribute).GetMethod(nameof(ResolveLazy),
                BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly ConcurrentDictionary<Type, 
                Func<object, object, object, object, object>>
            Lazy = new ConcurrentDictionary<Type,
                Func<object, object, object, object, object>>();

        public bool IsOptional => false;

        public virtual object ResolveArgument(Inquiry parent,
            Argument argument, IHandler handler, IHandler composer)
        {
            if (!argument.IsLazy)
                return Resolve(parent, argument, handler, composer);
            var lazy = Lazy.GetOrAdd(argument.ParameterType, l =>
            {
                var func   = l.GenericTypeArguments[0];
                var method = CreateLazy.MakeGenericMethod(func);
                return (Func<object, object, object, object, object>)
                    RuntimeHelper.CompileMethod(method, 
                        typeof(Func<object, object, object, object, object>));
            });
            return lazy(this, parent, argument, composer);
        }

        public virtual void ValidateArgument(Argument argument)
        {
        }

        protected virtual object Resolve(Inquiry parent,
            object key, IHandler handler, IHandler composer)
        {
            return handler.Resolve(key, parent);
        }

        protected virtual Promise ResolveAsync(Inquiry parent,
            object key, IHandler handler, IHandler composer)
        {
            return handler.ResolveAsync(key, parent);
        }

        protected virtual object[] ResolveAll(Inquiry parent,
            object key, IHandler handler, IHandler composer)
        {
            return handler.ResolveAll(key, parent);
        }

        protected virtual Promise<object[]> ResolveAllAsync(
            Inquiry parent, object key, IHandler handler,
            IHandler composer)
        {
            return handler.ResolveAllAsync(key, parent);
        }

        private Func<T> ResolveLazy<T>(
            Inquiry parent, Argument argument, IHandler composer)
        {
            return () => (T)Resolve(parent, argument, composer, composer);
        }

        private object Resolve(Inquiry parent, Argument argument,
            IHandler handler, IHandler composer)
        {
            object dependency;
            var key          = argument.Key;
            var argumentType = argument.ArgumentType;
            var logicalType  = argument.LogicalType;

            if (argument.IsArray)
            {
                if (argument.IsPromise)
                {
                    var array  = ResolveAllAsync(parent, key, handler, composer);
                    dependency = argument.IsSimple
                               ? array.Then((arr, s) =>
                                    RuntimeHelper.ChangeArrayType(arr, logicalType))
                                     .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else if (argument.IsTask)
                {
                    var array  = ResolveAllAsync(parent, key, handler, composer).ToTask();
                    dependency = argument.IsSimple
                               ? array.ContinueWith(task =>
                                    RuntimeHelper.ChangeArrayType(task.Result, logicalType),
                                TaskContinuationOptions.ExecuteSynchronously)
                                    .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else
                    dependency = RuntimeHelper.ChangeArrayType(
                        ResolveAll(parent, key, handler, composer), logicalType);
            }
            else if (argument.IsPromise)
            {
                var promise = ResolveAsync(parent, key, handler, composer);
                if (argument.IsSimple)
                    promise = promise.Then((r,s) => RuntimeHelper.ChangeType(r, logicalType));
               dependency = promise.Coerce(argumentType);
            }
            else if (argument.IsTask)
            {
                var task = ResolveAsync(parent, key, handler, composer).ToTask();
                if (argument.IsSimple)
                    task = task.ContinueWith(t => RuntimeHelper.ChangeType(t.Result, logicalType),
                        TaskContinuationOptions.ExecuteSynchronously);
                dependency = task.Coerce(argumentType);
            }
            else
            {
                dependency = Resolve(parent, key, handler, composer);
                if (argument.IsSimple)
                    dependency = RuntimeHelper.ChangeType(dependency, argumentType);
            }

            return dependency;
        }
    }
}
