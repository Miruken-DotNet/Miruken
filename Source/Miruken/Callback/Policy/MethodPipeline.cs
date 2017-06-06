namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Concurrency;

    internal abstract class MethodPipeline
    {
        public abstract bool Invoke(MethodBinding binding, object target,
            object callback, Func<IHandler, object> complete, IHandler composer,
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
            object callback, Func<IHandler, object> complete, IHandler composer,
            IEnumerable<IFilter> filters, out object result)
        {
            var completed = true;
            using (var pipeline = filters.GetEnumerator())
            {
                NextDelegate<object> next = null;
                next = (proceed, comp) =>
                {
                    if (!proceed)
                    {
                        completed = false;
                        return default(Res);
                    }
                    while (pipeline.MoveNext())
                    {
                        composer = comp ?? composer;
                        var filter      = pipeline.Current;
                        var typedFilter = filter as IFilter<Cb, Res>;
                        if (typedFilter != null)
                            return typedFilter.Next((Cb)callback, binding, composer, 
                                (p,c) => (Res)binding.CoerceResult(next(p, c), typeof(Res)));
                        var taskFilter = filter as IFilter<Cb, Task<Res>>;
                        if (taskFilter != null)
                            return taskFilter.Next((Cb)callback, binding, composer,
                                (p, c) => (Task<Res>)binding.CoerceResult(next(p, c),
                                           typeof(Task<Res>)));
                        var promiseFilter = filter as IFilter<Cb, Promise<Res>>;
                        if (promiseFilter != null)
                            return promiseFilter.Next((Cb)callback, binding, composer,
                                (p, c) => (Promise<Res>)binding.CoerceResult(next(p, c),
                                           typeof(Promise<Res>)));
                        var dynamicFilter = filter as IDynamicFilter;
                        if (dynamicFilter != null)
                            return (Res)dynamicFilter.Next(callback, binding, composer,
                                (p,c) => binding.CoerceResult(next(p, c), typeof(Res)));
                    }
                    return complete(composer);
                };
                result = next(true, composer);
                return completed;
            }
        }
    }
}
