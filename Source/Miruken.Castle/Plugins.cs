namespace Miruken.Castle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::Castle.Core.Internal;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;

    public class Plugins : IWindsorInstaller
    {
        private readonly Plugin[] _plugins;

        public Plugins(params Plugin[] plugins)
        {
            if (plugins == null)
                throw new ArgumentNullException(nameof(plugins));
            _plugins = plugins;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            foreach (var plugin in _plugins) container.Register(plugin);
        }

        public static Plugins InDirectory(AssemblyFilter filter)
        {
            var assemblies = new HashSet<Assembly>(ReflectionUtil.GetAssemblies(filter));
            var plugins    = assemblies.Select(Plugin.FromAssembly).ToArray();
            return new Plugins(plugins);
        }
    }
}
