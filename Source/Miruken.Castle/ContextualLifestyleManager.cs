using System;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;
namespace Miruken.Castle
{
    using Context;
    using Infrastructure;

    public class ContextualLifestyleManager : ScopedLifestyleManager
    {
        public ContextualLifestyleManager()
            : base(new ContextualScopeAccessor())
        {      
        }

        public override void Init(
            IComponentActivator componentActivator, IKernel kernel, ComponentModel model)
        {
            var implementation = model.Implementation;
            if (!implementation.Is<IContextual>())
            {
                throw new InvalidOperationException(
                    $"Component {implementation.FullName} must implement IContextual to use this lifestyle");
            }
            base.Init(componentActivator, kernel, model);
        }
    }
}
