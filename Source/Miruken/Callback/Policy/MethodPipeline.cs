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
            IEnumerable<IFilter> filters, out object result);

        public static MethodPipeline GetPipeline(Type callbackType, Type resultType)
        {
            if (resultType == typeof(void))
                resultType  = typeof(object);
            var key = Tuple.Create(callbackType, resultType);
            return _pipelines.GetOrAdd(key, k =>
                (MethodPipeline)Activator.CreateInstance(
                    typeof(MethodPipeline<,>).MakeGenericType(k.Item1, k.Item2)
            ));
        }

        private static readonly ConcurrentDictionary<Tuple<Type, Type>, MethodPipeline>
            _pipelines = new ConcurrentDictionary<Tuple<Type, Type>, MethodPipeline>();
    }

    internal class MethodPipeline<Cb, Res> : MethodPipeline
    {
        public override bool Invoke(MethodBinding binding, object target, 
            object callback, CompletePipelineDelegate complete, IHandler composer,
            IEnumerable<IFilter> filters, out object result)
        {
            var completed = true;
            using (var pipeline = filters.GetEnumerator())
            {
                Next<object> next = null;
                next = (proceed, comp) =>
                {
                    composer = comp ?? composer;
                    if (!proceed)
                    {
                        completed = false;
                        return default(Res);
                    }
                    while (pipeline.MoveNext())
                    {
                        var filter     = pipeline.Current;
                        var typeFilter = filter as IFilter<Cb, Res>;
                        if (typeFilter != null)
                            return typeFilter.Next((Cb)callback, binding, composer, 
                                (p,c) => (Res)binding.CoerceResult(next(p,c), typeof(Res)));
                        var taskFilter = filter as IFilter<Cb, Task<Res>>;
                        if (taskFilter != null)
                            return taskFilter.Next((Cb)callback, binding, composer,
                                (p,c) => (Task<Res>)binding.CoerceResult(next(p,c),
                                           typeof(Task<Res>)));
                        var promiseFilter = filter as IFilter<Cb, Promise<Res>>;
                        if (promiseFilter != null)
                            return promiseFilter.Next((Cb)callback, binding, composer,
                                (p,c) => (Promise<Res>)binding.CoerceResult(next(p,c),
                                           typeof(Promise<Res>)));
                    }
                    return complete(composer, out completed);
                };
                result = next();
                return completed;
            }
        }
    }
}
