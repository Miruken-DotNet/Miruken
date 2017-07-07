namespace Miruken.Castle.Tests
{
    using System.Configuration;
    using System.Reflection;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ConfigurationFactoryInstallerTests
    {
        private IWindsorContainer _container;

        public interface ITestConfig
        {
            [Update]
            int? Timeout { get; set; }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _container = new WindsorContainer()
                .Install(WithFeatures.FromAssembly(Assembly.GetExecutingAssembly()),
                         new ConfigurationFactoryInstaller());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
        }

        [TestMethod]
        public void Should_Create_Configuration()
        {
            var config = _container.Resolve<ITestConfig>();
            config.Timeout = 200;
            Assert.AreEqual(200, config.Timeout);
            Assert.AreEqual("200", ConfigurationManager.AppSettings["Timeout"]);
            config.Timeout = 300;
            Assert.AreEqual(300, config.Timeout);
            Assert.AreEqual("300", ConfigurationManager.AppSettings["Timeout"]);
            config.Timeout = null;
            Assert.AreEqual(null, config.Timeout);
            Assert.AreEqual("", ConfigurationManager.AppSettings["Timeout"]);
        }
    }
}
