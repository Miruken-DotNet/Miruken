namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal abstract class MethodPipeline
    {
        public abstract object Invoke(MethodBinding binding,
            object target, object callback, object[] args, Type returnType,
            IEnumerable<CallbackFilterAttribute> filters, IHandler composer,
            out bool handled);
    }

    internal class MethodPipeline<Cb, Res> : MethodPipeline
    {
        public override object Invoke(MethodBinding binding,
            object target, object callback, object[] args, Type returnType,
            IEnumerable<CallbackFilterAttribute> filters, IHandler composer,
            out bool handled)
        {
            var completed = false;
            using (var pipeline = GetPipeline(filters, composer).GetEnumerator())
            {
                CallbackDelegate<Res> next = null;
                next = proceed =>
                {
                    if (!proceed) return default(Res);
                    if (pipeline.MoveNext())
                        return pipeline.Current.Filter((Cb)callback, composer, next);
                    completed = true;
                    return (Res)binding.Dispatcher.Invoke(target, args, returnType);
                };

                var result = next();
                handled = completed;
                return result;
            }
        }

        private static IEnumerable<ICallbackFilter<Cb, Res>> GetPipeline(
            IEnumerable<CallbackFilterAttribute> filters, IHandler composer)
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
                .OfType<ICallbackFilter<Cb, Res>>();
        }
    }
}
