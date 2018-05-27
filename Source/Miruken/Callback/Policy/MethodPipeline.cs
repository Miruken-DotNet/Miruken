namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

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
            var key = (callbackType, resultType);
            return Pipelines.GetOrAdd(key, k =>
                (MethodPipeline)Activator.CreateInstance(
                    typeof(MethodPipeline<,>).MakeGenericType(k.Item1, k.Item2)
            ));
        }

        private static readonly ConcurrentDictionary<(Type, Type), MethodPipeline>
            Pipelines = new ConcurrentDictionary<(Type, Type), MethodPipeline>();
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
                Task<TRes> Next(IHandler comp, bool proceed)
                {
                    if (!proceed)
                    {
                        completed = false;
                        return Task.FromResult(default(TRes));
                    }

                    composer = comp ?? composer;
                    while (pipeline.MoveNext())
                    {
                        var (filter, provider) = pipeline.Current;
                        if (filter is IFilter<TCb, TRes> typedFilter)
                            return typedFilter.Next((TCb) callback, binding,
                                composer, Next, provider);
                    }

                    return (Task<TRes>) binding.CoerceResult(
                        complete(composer, out completed),
                        typeof(Task<TRes>));
                }

                result = Next(composer, true);
                return completed;
            }
        }
    }
}
