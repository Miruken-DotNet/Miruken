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
        public static readonly ResolvingAttribute Default = new ResolvingAttribute();

        private static readonly MethodInfo CreateLazy =
            typeof(ResolvingAttribute).GetMethod(nameof(ResolveLazy),
                BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly ConcurrentDictionary<Type, 
                Func<object, object, object, object, object>>
            Lazy = new ConcurrentDictionary<Type,
                Func<object, object, object, object, object>>();

        public bool IsOptional => false;

        public virtual object ResolveArgument(Inquiry parent,
            Argument argument, IHandler handler)
        {
            if (!argument.IsLazy)
                return Resolve(parent, argument, handler);
            var lazy = Lazy.GetOrAdd(argument.ParameterType, l =>
            {
                var func   = l.GenericTypeArguments[0];
                var method = CreateLazy.MakeGenericMethod(func);
                return (Func<object, object, object, object, object>)
                    RuntimeHelper.CompileMethod(method, 
                        typeof(Func<object, object, object, object, object>));
            });
            return lazy(this, parent, argument, handler);
        }

        public virtual void ValidateArgument(Argument argument)
        {
        }

        protected virtual object Resolve(
            Inquiry parent, Argument argument,
            object key, IHandler handler)
        {
            return handler.Resolve(new Inquiry(key, parent), argument.Constraints);
        }

        protected virtual Promise ResolveAsync(
            Inquiry parent, Argument argument,
            object key, IHandler handler)
        {
            return handler.ResolveAsync(new Inquiry(key, parent)
            {
                WantsAsync = true
            }, argument.Constraints).Then((result, _) =>
            {
                if (result == null && !argument.IsOptional)
                {
                    throw new InvalidOperationException(
                        $"Unable to resolve key '{key}'");
                }
                return result;
            });
        }

        protected virtual object[] ResolveAll(
            Inquiry parent, Argument argument,
            object key, IHandler handler)
        {
            return handler.ResolveAll(new Inquiry(key, parent, true),
                argument.Constraints);
        }

        protected virtual Promise<object[]> ResolveAllAsync(
            Inquiry parent, Argument argument,
            object key, IHandler handler)
        {
            return handler.ResolveAllAsync(new Inquiry(key, parent, true)
            {
                WantsAsync = true
            }, argument.Constraints);
        }

        private Func<T> ResolveLazy<T>(
            Inquiry parent, Argument argument, IHandler handler)
        {
            return () => (T)Resolve(parent, argument, handler);
        }

        private object Resolve(Inquiry parent,
            Argument argument, IHandler handler)
        {
            object dependency;
            var key          = argument.Key;
            var argumentType = argument.ArgumentType;
            var logicalType  = argument.LogicalType;

            if (argument.IsArray)
            {
                if (argument.IsPromise)
                {
                    var array = ResolveAllAsync(
                        parent, argument, key, handler);
                    dependency = argument.IsSimple
                               ? array.Then((arr, s) =>
                                    RuntimeHelper.ChangeArrayType(arr, logicalType))
                                     .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else if (argument.IsTask)
                {
                    var array  = ResolveAllAsync( parent, argument, key, handler).ToTask();
                    dependency = argument.IsSimple
                               ? array.ContinueWith(task =>
                                    RuntimeHelper.ChangeArrayType(task.Result, logicalType),
                                TaskContinuationOptions.ExecuteSynchronously)
                                    .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else
                    dependency = RuntimeHelper.ChangeArrayType(
                        ResolveAll(parent, argument, key, handler), logicalType);
            }
            else if (argument.IsPromise)
            {
                var promise = ResolveAsync(parent, argument, key, handler);
                if (argument.IsSimple)
                {
                    promise = promise.Then((r, s) =>
                        RuntimeHelper.ChangeType(r, logicalType));
                }
                dependency = promise.Coerce(argumentType);
            }
            else if (argument.IsTask)
            {
                var task = ResolveAsync(parent, argument, key, handler).ToTask();
                if (argument.IsSimple)
                {
                    task = task.ContinueWith(t =>
                            RuntimeHelper.ChangeType(t.Result, logicalType),
                        TaskContinuationOptions.ExecuteSynchronously);
                }
                dependency = task.Coerce(argumentType);
            }
            else
            {
                dependency = Resolve(parent, argument, key, handler);
                if (argument.IsSimple)
                    dependency = RuntimeHelper.ChangeType(dependency, argumentType);
            }

            if (argument.IsMaybe)
            {
                return dependency == null
                     ? Maybe.DynamicNothing(argument.ParameterType)
                     : Maybe.DynamicSome(dependency);
            }

            return dependency;
        }
    }
}
