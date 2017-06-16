namespace Miruken.Castle
{
    using System.Linq;
    using global::Castle.Core.Internal;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.Windsor.Installer;

    public static class Plugins
    {
        public static IWindsorInstaller InDirectory(AssemblyFilter filter)
        {
            var installer = new CompositeInstaller();
            var plugins   = ReflectionUtil.GetAssemblies(filter);
            foreach (var plugin in plugins.Distinct())
                installer.Add(Plugin.FromAssembly(plugin));
            return installer;
        }
    }
}
