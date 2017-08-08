namespace Miruken.Castle.Tests
{
    using global::Castle.MicroKernel.Registration;
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
            _container.Install(myInstaller,
                WithFeatures.From(Classes.FromThisAssembly()));
            Assert.IsTrue(myInstaller.Installed);
            Assert.AreEqual(1, myInstaller.Count);
        }

        [TestMethod]
        public void Should_Process_New_Features()
        {
            var myInstaller = new MyInstaller();
            _container.Install(myInstaller);
            Assert.IsTrue(myInstaller.Installed);
            Assert.IsTrue(myInstaller.Count == 0);
            _container.Install(
                WithFeatures.From(Classes.FromThisAssembly()));
            Assert.IsTrue(myInstaller.Count == 1);
        }

        public class MyInstaller : FeatureInstaller
        {
            public bool Installed { get; set; }
            public int  Count { get; set; }

            protected override void Install(IConfigurationStore store)
            {
                Installed = true;
            }

            public override void InstallFeatures(FromDescriptor from)
            {
                ++Count;
            }
        }
    }
}
