namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
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

        private static readonly ConcurrentDictionary<Type, TwoArgsReturnDelegate>
            _lazy = new ConcurrentDictionary<Type, TwoArgsReturnDelegate>();

        public bool IsOptional => false;

        public virtual object ResolveArgument(
            Argument argument, IHandler handler, IHandler composer)
        {
            if (argument.IsLazy)
            {
                var lazy = _lazy.GetOrAdd(argument.ParameterType, l =>
                {
                    var func   = l.GenericTypeArguments[0];
                    var method = CreateLazy.MakeGenericMethod(func);
                    return RuntimeHelper.CreateFuncTwoArgs(method);
                });
                return lazy(this, argument, composer);
            }
            return Resolve(argument, handler);
        }

        public virtual void ValidateArgument(Argument argument)
        {
        }

        protected virtual object Resolve(object key, IHandler handler)
        {
            return handler.Resolve(key);
        }

        protected virtual Promise ResolveAsync(object key, IHandler handler)
        {
            return handler.ResolveAsync(key);
        }

        protected virtual object[] ResolveAll(object key, IHandler handler)
        {
            return handler.ResolveAll(key);
        }

        protected virtual Promise<object[]> ResolveAllAsync(object key, IHandler handler)
        {
            return handler.ResolveAllAsync(key);
        }

        private Func<T> ResolveLazy<T>(Argument argument, IHandler handler)
        {
            return () => (T)Resolve(argument, handler);
        }

        private object Resolve(Argument argument, IHandler handler)
        {
            object dependency;
            var key          = argument.Key;
            var argumentType = argument.ArgumentType;
            var logicalType  = argument.LogicalType;

            if (argument.IsArray)
            {
                if (argument.IsPromise)
                {
                    var array  = ResolveAllAsync(key, handler);
                    dependency = argument.IsSimple
                               ? array.Then((arr, s) =>
                                    RuntimeHelper.ChangeArrayType(arr, logicalType))
                                     .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else if (argument.IsTask)
                {
                    var array  = ResolveAllAsync(key, handler).ToTask();
                    dependency = argument.IsSimple
                               ? array.ContinueWith(task =>
                                    RuntimeHelper.ChangeArrayType(task.Result, logicalType))
                                    .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else
                    dependency = RuntimeHelper
                        .ChangeArrayType(ResolveAll(key, handler), logicalType);
            }
            else if (argument.IsPromise)
            {
                var promise = ResolveAsync(key, handler);
                if (argument.IsSimple)
                    promise = promise.Then((r,s) => RuntimeHelper.ChangeType(r, logicalType));
               dependency = promise.Coerce(argumentType);
            }
            else if (argument.IsTask)
            {
                var task = ResolveAsync(key, handler).ToTask();
                if (argument.IsSimple)
                    task = task.ContinueWith(t => RuntimeHelper.ChangeType(t.Result, logicalType));
                dependency = task.Coerce(argumentType);
            }
            else
            {
                dependency = Resolve(key, handler);
                if (argument.IsSimple)
                    dependency = RuntimeHelper.ChangeType(dependency, argumentType);
            }

            return dependency;
        }
    }
}
