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
            _container.Install(new FeaturesInstaller(
                myInstaller).Use(Classes.FromThisAssembly()));
            Assert.IsTrue(myInstaller.Installed);
            Assert.AreEqual(1, myInstaller.Count);
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
