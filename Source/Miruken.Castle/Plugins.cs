namespace Miruken.Castle
{
    using System.Linq;
    using System.Reflection;
    using global::Castle.Core.Internal;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.Windsor.Installer;

    public static class Plugins
    {
        public static IWindsorInstaller FromAssemblies(params Assembly[] assemblies)
        {
            var installer = new CompositeInstaller();
            foreach (var assembly in assemblies)
                installer.Add(Plugin.FromAssembly(assembly));
            return installer;
        }

        public static IWindsorInstaller FromAssembliesNamed(params string[] assemblyNames)
        {
            var installer = new CompositeInstaller();
            foreach (var assemblyName in assemblyNames)
                installer.Add(Plugin.FromAssemblyNamed(assemblyName));
            return installer;
        }

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
