namespace Miruken.Callback
{
    using System;
    using System.Threading.Tasks;
    using Concurrency;
    using Functional;
    using Infrastructure;
    using Policy;

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ResolvingAttribute : Attribute, IArgumentResolver
    {
        public static readonly ResolvingAttribute Default = new ResolvingAttribute();

        public bool IsOptional => false;

        public virtual object ResolveArgument(Inquiry parent,
            Argument argument, IHandler handler)
        {
            return Resolve(parent, argument, handler, false);
        }

        public object ResolveArgumentAsync(Inquiry parent,
            Argument argument, IHandler handler)
        {
            return Resolve(parent, argument, handler, true);
        }

        public virtual void ValidateArgument(Argument argument)
        {
        }

        protected virtual object Resolve(Inquiry parent,
            Argument argument, object key, IHandler handler)
        {
            return handler.Resolve(new Inquiry(key, parent), argument.Constraints);
        }

        protected virtual Promise ResolveAsync(Inquiry parent,
            Argument argument, object key, IHandler handler)
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
            return () => (T)Resolve(parent, argument, handler, false);
        }

        private object Resolve(Inquiry parent, Argument argument,
            IHandler handler, bool wantsAsync)
        {
       
            object dependency;
            var key          = argument.Key;
            var argumentType = argument.ArgumentType;
            var logicalType  = argument.LogicalType;

            if (argument.IsEnumerable)
            {
                if (argument.IsPromise)
                {
                    var array = ResolveAllAsync(parent, argument, key, handler);
                    dependency = argument.IsSimple
                               ? array.Then((arr, s) =>
                                    RuntimeHelper.ChangeArrayType(arr, logicalType))
                                     .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else  if (argument.IsTask)
                {
                    var array  = ResolveAllAsync(parent, argument, key, handler).ToTask();
                    dependency = argument.IsSimple
                               ? array.ContinueWith(task =>
                                    RuntimeHelper.ChangeArrayType(task.Result, logicalType),
                                TaskContinuationOptions.ExecuteSynchronously)
                                    .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else if (wantsAsync)
                {
                    dependency = ResolveAllAsync(parent, argument, key, handler)
                        .Then((array, _) => RuntimeHelper.ChangeArrayType(array, logicalType));
                }
                else
                {
                    dependency = RuntimeHelper.ChangeArrayType(
                        ResolveAll(parent, argument, key, handler), logicalType);
                }
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
            else if (wantsAsync && !argument.IsMaybe)
            {
                dependency = ResolveAsync(parent, argument, key, handler);
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
