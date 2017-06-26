namespace Miruken.Castle.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FeaturesTests
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
        public void Should_Process_Existing_Features()
        {
            var myInstaller = new MyInstaller();
            var assembly    = Assembly.GetExecutingAssembly();
            _container.Install(Features.FromAssembly(assembly), myInstaller);
            Assert.IsTrue(myInstaller.Installed);
            CollectionAssert.AreEqual(
                new [] { Features.FromAssembly(assembly) },
                myInstaller.InstalledFeatures);
        }

        [TestMethod]
        public void Should_Process_New_Features()
        {
            var myInstaller = new MyInstaller();
            var assembly    = Assembly.GetExecutingAssembly();
            _container.Install(myInstaller);
            Assert.IsTrue(myInstaller.Installed);
            Assert.IsTrue(myInstaller.InstalledFeatures.Length == 0);
            _container.Install(Features.FromAssembly(assembly));
            CollectionAssert.AreEqual(
                new[] { Features.FromAssembly(assembly) },
                myInstaller.InstalledFeatures);
        }

        [TestMethod]
        public void Should_Ignore_Duplicate_Features()
        {
            var myInstaller = new MyInstaller();
            var assembly    = Assembly.GetExecutingAssembly();
            _container.Install(
                Features.FromAssembly(assembly), 
                Features.FromAssembly(assembly),
                myInstaller);
            Assert.IsTrue(myInstaller.Installed);
            _container.Install(Features.FromAssembly(assembly));
            CollectionAssert.AreEqual(
                new[] { Features.FromAssembly(assembly) },
                myInstaller.InstalledFeatures);
        }

        [TestMethod,
         ExpectedException(typeof(FileNotFoundException))]
        public void Should_Reject_Invalid_Feature()
        {
            Features.FromAssemblyNamed("foo");
        }

        public class MyInstaller : FeatureInstaller
        {
            private readonly List<FeatureAssembly> _features = new List<FeatureAssembly>();

            public bool Installed { get; set; }

            public FeatureAssembly[] InstalledFeatures => _features.ToArray();

            protected override void Install(IConfigurationStore store)
            {
                Installed = true;
            }

            protected override void InstallFeature(FeatureAssembly feature)
            {
                _features.Add(feature);
            }
        }
    }
}
