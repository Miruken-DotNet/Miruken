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
            Lazy = new ConcurrentDictionary<Type, TwoArgsReturnDelegate>();

        public bool IsOptional => false;

        public virtual object ResolveArgument(
            Argument argument, IHandler handler, IHandler composer)
        {
            if (!argument.IsLazy)
                return Resolve(argument, handler, composer);
            var lazy = Lazy.GetOrAdd(argument.ParameterType, l =>
            {
                var func   = l.GenericTypeArguments[0];
                var method = CreateLazy.MakeGenericMethod(func);
                return RuntimeHelper.CreateFuncTwoArgs(method);
            });
            return lazy(this, argument, composer);
        }

        public virtual void ValidateArgument(Argument argument)
        {
        }

        protected virtual object Resolve(
            object key, IHandler handler, IHandler composer)
        {
            return handler.Resolve(key);
        }

        protected virtual Promise ResolveAsync(
            object key, IHandler handler, IHandler composer)
        {
            return handler.ResolveAsync(key);
        }

        protected virtual object[] ResolveAll(
            object key, IHandler handler, IHandler composer)
        {
            return handler.ResolveAll(key);
        }

        protected virtual Promise<object[]> ResolveAllAsync(
            object key, IHandler handler, IHandler composer)
        {
            return handler.ResolveAllAsync(key);
        }

        private Func<T> ResolveLazy<T>(Argument argument, IHandler composer)
        {
            return () => (T)Resolve(argument, composer, composer);
        }

        private object Resolve(
            Argument argument, IHandler handler, IHandler composer)
        {
            object dependency;
            var key          = argument.Key;
            var argumentType = argument.ArgumentType;
            var logicalType  = argument.LogicalType;

            if (argument.IsArray)
            {
                if (argument.IsPromise)
                {
                    var array  = ResolveAllAsync(key, handler, composer);
                    dependency = argument.IsSimple
                               ? array.Then((arr, s) =>
                                    RuntimeHelper.ChangeArrayType(arr, logicalType))
                                     .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else if (argument.IsTask)
                {
                    var array  = ResolveAllAsync(key, handler, composer).ToTask();
                    dependency = argument.IsSimple
                               ? array.ContinueWith(task =>
                                    RuntimeHelper.ChangeArrayType(task.Result, logicalType))
                                    .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else
                    dependency = RuntimeHelper
                        .ChangeArrayType(ResolveAll(key, handler, composer), logicalType);
            }
            else if (argument.IsPromise)
            {
                var promise = ResolveAsync(key, handler, composer);
                if (argument.IsSimple)
                    promise = promise.Then((r,s) => RuntimeHelper.ChangeType(r, logicalType));
               dependency = promise.Coerce(argumentType);
            }
            else if (argument.IsTask)
            {
                var task = ResolveAsync(key, handler, composer).ToTask();
                if (argument.IsSimple)
                    task = task.ContinueWith(t => RuntimeHelper.ChangeType(t.Result, logicalType));
                dependency = task.Coerce(argumentType);
            }
            else
            {
                dependency = Resolve(key, handler, composer);
                if (argument.IsSimple)
                    dependency = RuntimeHelper.ChangeType(dependency, argumentType);
            }

            return dependency;
        }
    }
}
