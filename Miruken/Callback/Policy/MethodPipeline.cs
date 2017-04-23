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
            IEnumerable<IFilter> filters, out object result);
    }

    internal class MethodPipeline<Cb, Res> : MethodPipeline
    {
        public override bool Invoke(MethodBinding binding,
            object target, object callback, object[] args, 
            Type returnType, IHandler composer,
            IEnumerable<IFilter> filters, out object result)
        {
            var completed = false;
            using (var pipeline = filters.OfType<IFilter<Cb, Res>>().GetEnumerator())
            {
                FilterDelegate<Res> next = null;
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
    }
}
