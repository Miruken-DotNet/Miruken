namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;

    public delegate bool ResultsDelegate(object result, bool strict, int? priority = null);

    public abstract class MemberBinding : FilteredObject
    {
        protected MemberBinding(MemberDispatch dispatch)
        {
            Dispatcher = dispatch 
                ?? throw new ArgumentNullException(nameof(dispatch));
            AddFilters(dispatch.Attributes.OfType<IFilterProvider>().ToArray());
        }

        public MemberDispatch Dispatcher { get; }

        public abstract bool Dispatch(object target, object callback,
            IHandler composer, int? priority = null, ResultsDelegate results = null);

        internal object CoerceResult(object result, Type resultType, bool? wantsAsync = null)
        {
            if (wantsAsync == true)
            {
                var promise = result as Promise;
                return promise ?? Promise.Resolved(result);
            }
            if (result == null || !resultType.IsInstanceOfType(result))
            {
                if (resultType.Is<Task>())
                {
                    switch (result)
                    {
                        case Task task:
                            return task.Coerce(resultType);
                        case Promise promise:
                            return promise.ToTask().Coerce(resultType);
                        default:
                            return Task.FromResult(result).Coerce(resultType);
                    }
                }
                if (resultType.Is<Promise>())
                    return Promise.Resolved(result).Coerce(resultType);
                else
                {
                    var promise = result as Promise;
                    if (promise == null)
                    {
                        if (result is Task task)
                            promise = Promise.Resolved(task);
                    }
                    if (promise != null)
                        return promise.Wait();
                }
            }
            return result;
        }
    }
}
