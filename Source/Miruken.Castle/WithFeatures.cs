namespace Miruken.Castle
{
    using System;
    using System.Linq;
    using System.Reflection;
    using global::Castle.Core.Internal;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.Windsor.Installer;

    public static class WithFeatures
    {
        public static FeatureAssembly FromAssembly(
            Assembly assembly, Action<FeatureAssembly> configrue = null)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            var feature = new FeatureAssembly(assembly);
            configrue?.Invoke(feature);
            return feature;
        }

        public static FeatureAssembly FromAssemblyNamed(
            string assemblyName, Action<FeatureAssembly> configrue = null)
        {
            var assembly = ReflectionUtil.GetAssemblyNamed(assemblyName);
            var feature  = new FeatureAssembly(assembly);
            configrue?.Invoke(feature);
            return feature;
        }

        public static IWindsorInstaller FromAssemblies(params Assembly[] assemblies)
        {
            var installer = new CompositeInstaller();
            foreach (var assembly in assemblies)
                installer.Add(FromAssembly(assembly));
            return installer;
        }

        public static IWindsorInstaller FromAssembliesNamed(params string[] assemblyNames)
        {
            var installer = new CompositeInstaller();
            foreach (var assemblyName in assemblyNames)
                installer.Add(FromAssemblyNamed(assemblyName));
            return installer;
        }

        public static IWindsorInstaller InDirectory(AssemblyFilter filter)
        {
            var installer = new CompositeInstaller();
            var features  = ReflectionUtil.GetAssemblies(filter);
            foreach (var feature in features.Distinct())
                installer.Add(FromAssembly(feature));
            return installer;
        }

        public static IWindsorInstaller InDirectory(string directory)
        {
            return InDirectory(new AssemblyFilter(directory));
        }
    }
}
