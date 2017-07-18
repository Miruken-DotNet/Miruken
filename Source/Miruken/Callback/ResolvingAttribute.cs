namespace Miruken.Callback
{
    using System;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;
    using Policy;

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ResolvingAttribute : Attribute, IArgumentResolver
    {
        public virtual object ResolveArgument(Argument argument, IHandler handler)
        {
            object dependency;
            var key          = argument.Key;
            var argumentType = argument.ArgumentType;
            var logicalType  = argument.LogicalType;

            if (argument.IsArray)
            {
                if (argument.IsPromise)
                {
                    var array  = handler.ResolveAllAsync(key);
                    dependency = argument.IsSimple
                               ? array.Then((arr, s) =>
                                    RuntimeHelper.ChangeArrayType(arr, logicalType))
                                     .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else if (argument.IsTask)
                {
                    var array  = handler.ResolveAllAsync(key).ToTask();
                    dependency = argument.IsSimple
                               ? array.ContinueWith(task =>
                                    RuntimeHelper.ChangeArrayType(task.Result, logicalType))
                                    .Coerce(argumentType)
                               : array.Coerce(argumentType);
                }
                else
                    dependency = RuntimeHelper
                        .ChangeArrayType(handler.ResolveAll(key), logicalType);
            }
            else if (argument.IsPromise)
            {
                var promise = handler.ResolveAsync(key);
                if (argument.IsSimple)
                    promise = promise.Then((r,s) => RuntimeHelper.ChangeType(r, logicalType));
               dependency = promise.Coerce(argumentType);
            }
            else if (argument.IsTask)
            {
                var task = handler.ResolveAsync(key).ToTask();
                if (argument.IsSimple)
                    task = task.ContinueWith(t => RuntimeHelper.ChangeType(t.Result, logicalType));
                dependency = task.Coerce(argumentType);
            }
            else
            {
                dependency = handler.Resolve(key);
                if (argument.IsSimple)
                    dependency = RuntimeHelper.ChangeType(dependency, argumentType);
            }

            return dependency;
        }
    }
}
