namespace Miruken.Mediator
{
    using System;
    using Callback;

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
    }
}