using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
using Miruken.Callback;

namespace Miruken.Castle
{
    using IHandler = Callback.IHandler;

    public class ExternalDependencyResolver : ISubDependencyResolver
    {
        public bool CanResolve(
            CreationContext context, ISubDependencyResolver contextHandlerResolver,
            ComponentModel model, DependencyModel dependency)
        {
            if (dependency.IsPrimitiveTypeDependency)
                return false;
            var extra = context.AdditionalArguments;
            if (extra.Contains(dependency)) return true;
            var composer = extra[WindsorHandler.ComposerKey] as IHandler;
            if (composer == null) return false;
            var parent     = extra[WindsorHandler.ResolutionKey] as DependencyResolution;
            var resolution = new DependencyResolution(dependency.TargetItemType, parent);
            if (composer.Handle(resolution))
            {
                extra[dependency] = resolution;
                return true;
            }
            return false;
        }

        public object Resolve(
            CreationContext context, ISubDependencyResolver contextHandlerResolver,
            ComponentModel model, DependencyModel dependency)
        {
            var extra      = context.AdditionalArguments;
            var resolution = extra[dependency] as Inquiry;
            return resolution?.Result;
        }
    }
}
