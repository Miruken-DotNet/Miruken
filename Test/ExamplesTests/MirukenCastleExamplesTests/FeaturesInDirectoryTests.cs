namespace ExampleTests.MirukenCastleExamplesTests
{
    using Example.mirukenCastleExamples;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FeaturesInDirectoryTests
    {
        [TestMethod]
        public void CanConfigureContainer()
        {
            var container = new FeaturesInDirectory().Container;
            Assert.IsNotNull(container);
        }
    }
}
