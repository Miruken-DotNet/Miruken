using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Miruken.Callback;

namespace Miruken.Castle
{
    public class ResolvingInstaller : IWindsorInstaller
    {
        private readonly FromAssemblyDescriptor[] _fromAssemblies;

        public ResolvingInstaller(params FromAssemblyDescriptor[] fromAssemblies)
        {
            _fromAssemblies = fromAssemblies;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            foreach (var assemebly in _fromAssemblies)
            {
                container.Register(assemebly.BasedOn(typeof(IResolving))
                    .WithServiceFromInterface()
                    );
            }
        }
    }
}
