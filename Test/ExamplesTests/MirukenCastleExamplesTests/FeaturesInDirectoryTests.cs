namespace ExampleTests.MirukenCastleExamplesTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Example.MirukenCastleExamples;
    using Miruken.Castle;

    [TestClass]
    public class FeaturesInDirectoryTests
    {
        [TestMethod]
        public void CanConfigureContainer()
        {
            var container = new FeaturesInDirectory().Container;
            Assert.IsNotNull(container);
            var featureAssemblies = container.ResolveAll<FeatureAssembly>();

            Assert.AreEqual(2, featureAssemblies.Length);
        }
    }
}
