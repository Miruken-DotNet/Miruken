namespace Miruken.Callback
{
    using System;
    using Infrastructure;
    using Policy;

    [AttributeUsage(AttributeTargets.Parameter)]
    public class BatchAttribute : Attribute, IArgumentResolver
    {
        public bool IsOptional => true;

        public BatchAttribute()
        {           
        }

        public BatchAttribute(object tag)
        {
            Tag = tag;
        }

        public object Tag { get; }

        public void ValidateArgument(Argument argument)
        {
            var batchType = argument.ParameterType;

            if (batchType != typeof(Batch))
            {
                if (!batchType.IsClass)
                    throw new NotSupportedException(
                        "Batch parameters must be classes");

                if (!batchType.Is<IBatching>())
                throw new NotSupportedException(
                    $"Batch parameters must implement {typeof(IBatching)}");

                if (!batchType.HasDefaultConstructor())
                    throw new NotSupportedException(
                        $"Batch parameter {batchType} does not have a default public constructor");
            }
            if (argument.IsPromise || argument.IsTask)
                throw new NotSupportedException(
                    "Batch parameters cannot be tasks or promises");
        }

        public object ResolveArgument(Argument argument, 
            IHandler handler, IHandler composer)
        {
            var batch = composer.GetBatch(Tag);
            if (batch != null && argument.ParameterType != typeof(Batch))
            {
                var batchType     = argument.ParameterType;
                var batchInstance = Activator.CreateInstance(batchType);
                batch.AddHandlers(batchInstance);
                return batchInstance;
            }
            return batch;
        }
    }
}
