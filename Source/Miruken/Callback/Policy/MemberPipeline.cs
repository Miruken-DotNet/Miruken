﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Bindings;

    public delegate object CompletePipelineDelegate(
        IHandler handler, out bool completed);

    public abstract class MemberPipeline
    {
        public abstract bool Invoke(
            MemberBinding binding, object target, object callback,
            object rawCallback, CompletePipelineDelegate complete,
            IHandler composer, IEnumerable<(IFilter, IFilterProvider)> filters, 
            out object result);

        public static MemberPipeline GetPipeline(Type callbackType, Type resultType)
        {
            if (resultType == typeof(void))
                resultType = typeof(object);
            var key = (callbackType, resultType);
            return Pipelines.GetOrAdd(key, k =>
                (MemberPipeline)Activator.CreateInstance(
                    typeof(MemberPipeline<,>).MakeGenericType(k.Item1, k.Item2)
            ));
        }

        private static readonly ConcurrentDictionary<(Type, Type), MemberPipeline>
            Pipelines = new();
    }

    public class MemberPipeline<TCb, TRes> : MemberPipeline
    {
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public override bool Invoke(
            MemberBinding binding, object target, object callback,
            object rawCallback, CompletePipelineDelegate complete,
            IHandler composer, IEnumerable<(IFilter, IFilterProvider)> filters,
            out object result)
        {
            var completed = true;
            using var pipeline = filters.GetEnumerator();

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
                        return typedFilter.Next((TCb)callback, rawCallback,
                            binding, composer, Next, provider);
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
