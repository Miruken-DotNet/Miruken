namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal abstract class MethodPipeline
    {
        public abstract bool Invoke(MethodBinding binding,
            object target, object callback, object[] args, 
            Type returnType, IHandler composer,
            out object result);
    }

    internal class MethodPipeline<Cb, Res> : MethodPipeline
    {
        public override bool Invoke(MethodBinding binding,
            object target, object callback, object[] args, 
            Type returnType, IHandler composer,
            out object result)
        {
            var completed = false;
            using (var pipeline = GetPipeline(binding.Filters, composer).GetEnumerator())
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
            IEnumerable<IPipleineFilterProvider> providers, IHandler composer)
        {
            composer = composer.Provide(ResolveOpenFilters);
            return providers
                .SelectMany(provider => provider.GetPipelineFilters(composer))
                .OrderByDescending(f => f.Order ?? int.MaxValue)             
                .OfType<IPieplineFilter<Cb, Res>>();
        }

        private static bool ResolveOpenFilters(
            Resolution resolution, IHandler composer)
        {
            var filterType = resolution.Key as Type;
            if (filterType?.IsGenericTypeDefinition != true ||
                !typeof(IPipelineFilter).IsAssignableFrom(filterType))
                return false;
            filterType = filterType.MakeGenericType(typeof(Cb), typeof(Res));
            if (resolution.Many)
            {
                var filters = composer.ResolveAll(filterType);
                if (filters == null || filters.Length == 0)
                    return false;
                foreach (var filter in filters)
                    resolution.Resolve(filter, composer);
            }
            else
            {
                var filter = composer.Resolve(filterType);
                if (filter == null) return false;
                resolution.Resolve(filter, composer);
            }
            return true;
        }
    }
}
