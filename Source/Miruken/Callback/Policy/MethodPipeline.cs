namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Concurrency;

    public delegate object CompletePipelineDelegate(
        IHandler handler, out bool completed);

    internal abstract class MethodPipeline
    {
        public abstract bool Invoke(MethodBinding binding, object target,
            object callback, CompletePipelineDelegate complete, IHandler composer,
            IEnumerable<(IFilter, IFilterProvider)> filters, out object result);

        public static MethodPipeline GetPipeline(Type callbackType, Type resultType)
        {
            if (resultType == typeof(void))
                resultType  = typeof(object);
            var key = Tuple.Create(callbackType, resultType);
            return Pipelines.GetOrAdd(key, k =>
                (MethodPipeline)Activator.CreateInstance(
                    typeof(MethodPipeline<,>).MakeGenericType(k.Item1, k.Item2)
            ));
        }

        private static readonly ConcurrentDictionary<Tuple<Type, Type>, MethodPipeline>
            Pipelines = new ConcurrentDictionary<Tuple<Type, Type>, MethodPipeline>();
    }

    internal class MethodPipeline<TCb, TRes> : MethodPipeline
    {
        public override bool Invoke(MethodBinding binding, object target, 
            object callback, CompletePipelineDelegate complete, IHandler composer,
            IEnumerable<(IFilter, IFilterProvider)> filters, out object result)
        {
            var completed = true;
            using (var pipeline = filters.GetEnumerator())
            {
                Next<object> next = null;
                next = (comp, proceed) =>
                {
                    if (!proceed)
                    {
                        completed = false;
                        return default(TRes);
                    }
                    composer = comp ?? composer;
                    while (pipeline.MoveNext())
                    {
                        var (filter, provider) = pipeline.Current;
                        switch (filter)
                        {
                            case IFilter<TCb, TRes> typeFilter:
                                return typeFilter.Next((TCb)callback, binding, composer, 
                                    (p,c) => (TRes)binding.CoerceResult(next(p,c), typeof(TRes)),
                                    provider);
                            case IFilter<TCb, Task<TRes>> taskFilter:
                                return taskFilter.Next((TCb)callback, binding, composer,
                                    (p,c) => (Task<TRes>)binding.CoerceResult(next(p,c),
                                        typeof(Task<TRes>)), provider);
                            case IFilter<TCb, Promise<TRes>> promiseFilter:
                                return promiseFilter.Next((TCb)callback, binding, composer,
                                    (p,c) => (Promise<TRes>)binding.CoerceResult(next(p,c),
                                        typeof(Promise<TRes>)), provider);
                        }
                    }
                    return complete(composer, out completed);
                };
                result = next();
                return completed;
            }
        }
    }
}
