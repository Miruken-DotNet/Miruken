namespace Miruken.Mediator
{
    using System;
    using Callback;
    using Infrastructure;

    public class PipelineAttribute : FilterAttribute
    {
        public PipelineAttribute()
            : base(typeof(IPipelineBehavior<,>))
        {         
        }

        public PipelineAttribute(params Type[] behaviors)
            : base(behaviors)
        {            
        }

        protected override void VerifyFilterType(Type filterType)
        {
            var conformance = filterType.GetOpenTypeConformance(typeof(IPipelineBehavior<,>));
            if (conformance == null)
                throw new ArgumentException($"{filterType.FullName} does not conform to IPipelineBehavior<,>");
            base.VerifyFilterType(filterType);
        }
    }
}