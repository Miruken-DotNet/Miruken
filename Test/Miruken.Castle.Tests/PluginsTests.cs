namespace Miruken.Castle.Tests
{
    using System.Collections.Generic;
    using System.Reflection;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PluginsTests
    {
        protected IWindsorContainer _container;

        [TestInitialize]
        public void TestInitialize()
        {
            _container = new WindsorContainer();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
        }

        [TestMethod]
        public void Should_Process_Existing_Plugins()
        {
            var myInstaller = new MyInstaller();
            var assembly    = Assembly.GetExecutingAssembly();
            _container.Install(new Plugins(
                Plugin.FromAssembly(assembly)),
                myInstaller);
            Assert.IsTrue(myInstaller.Installed);
            CollectionAssert.AreEqual(
                new [] { Plugin.FromAssembly(assembly) },
                myInstaller.InstalledPlugins);
        }

        [TestMethod]
        public void Should_Process_New_Plugins()
        {
            var myInstaller = new MyInstaller();
            var assembly    = Assembly.GetExecutingAssembly();
            _container.Install(new Plugins(), myInstaller);
            Assert.IsTrue(myInstaller.Installed);
            Assert.IsTrue(myInstaller.InstalledPlugins.Length == 0);
            _container.Register(Plugin.FromAssembly(assembly));
            CollectionAssert.AreEqual(
                new[] { Plugin.FromAssembly(assembly) },
                myInstaller.InstalledPlugins);
        }

        [TestMethod]
        public void Should_Ignore_Duplicate_Plugins()
        {
            var myInstaller = new MyInstaller();
            var assembly    = Assembly.GetExecutingAssembly();
            _container.Install(new Plugins(
                Plugin.FromAssembly(assembly), 
                Plugin.FromAssembly(assembly)),
                myInstaller);
            Assert.IsTrue(myInstaller.Installed);
            _container.Register(Plugin.FromAssembly(assembly));
            CollectionAssert.AreEqual(
                new[] { Plugin.FromAssembly(assembly) },
                myInstaller.InstalledPlugins);

        }
        public class MyInstaller : PluginInstaller
        {
            private readonly List<Plugin> _plugins = new List<Plugin>();

            public bool Installed { get; set; }

            public Plugin[] InstalledPlugins => _plugins.ToArray();

            protected override void Install(IWindsorContainer container, IConfigurationStore store)
            {
                Installed = true;
            }

            protected override void InstallPlugin(Plugin plugin)
            {
                _plugins.Add(plugin);
            }
        }
    }
}
