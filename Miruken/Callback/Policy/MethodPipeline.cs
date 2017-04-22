namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal abstract class MethodPipeline
    {
        public abstract bool Invoke(MethodBinding binding,
            object target, object callback, object[] args, Type returnType,
            IEnumerable<PipelineAttribute> filters, IHandler composer,
            out object result);
    }

    internal class MethodPipeline<Cb, Res> : MethodPipeline
    {
        public override bool Invoke(MethodBinding binding,
            object target, object callback, object[] args, Type returnType,
            IEnumerable<PipelineAttribute> filters, IHandler composer,
            out object result)
        {
            var completed = false;
            using (var pipeline = GetPipeline(filters, composer).GetEnumerator())
            {
                PipelineDelegate<Res> next = null;
                next = proceed =>
                {
                    if (!proceed) return default(Res);
                    if (pipeline.MoveNext())
                        return pipeline.Current.Filter((Cb)callback, composer, next);
                    completed = true;
                    return (Res)binding.Dispatcher.Invoke(target, args, returnType);
                };

                result = next();
                return completed;
            }
        }

        private static IEnumerable<IPieplineFilter<Cb, Res>> GetPipeline(
            IEnumerable<PipelineAttribute> filters, IHandler composer)
        {
            return filters.SelectMany(filter =>
                filter.FilterTypes.SelectMany(filterType =>
                {
                    if (filterType.IsGenericTypeDefinition)
                        filterType = filterType.MakeGenericType(typeof(Cb), typeof(Res));
                    return filter.Many
                         ? composer.ResolveAll(filterType)
                         : new[] {composer.Resolve(filterType)};
                }))
                .OfType<IPieplineFilter<Cb, Res>>();
        }
    }
}
