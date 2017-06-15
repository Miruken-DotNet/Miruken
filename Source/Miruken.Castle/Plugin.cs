namespace Miruken.Castle
{
    using System;
    using System.Reflection;
    using global::Castle.Core.Internal;
    using global::Castle.MicroKernel;
    using global::Castle.MicroKernel.Registration;

    public class Plugin : IRegistration, IEquatable<Plugin>
    {
        private Plugin(Assembly assembly)
        {
            Assembly = assembly;
        }

        public Assembly Assembly { get; }

        void IRegistration.Register(IKernelInternal kernel)
        {
            var name = Assembly.FullName;
            if (kernel.HasComponent(name)) return;
            kernel.Register(Component.For<Plugin>().Instance(this).Named(name));
        }

        public bool Equals(Plugin other)
        {
            if (other == null) return false;
            return ReferenceEquals(this, other) || Equals(Assembly, other.Assembly);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Plugin);
        }

        public override int GetHashCode()
        {
            return Assembly.GetHashCode();
        }

        public static Plugin FromAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            return new Plugin(assembly);
        }

        public static Plugin FromAssemblyNamed(string assemblyName)
        {
            var assembly = ReflectionUtil.GetAssemblyNamed(assemblyName);
            return new Plugin(assembly);
        }
    }
}
