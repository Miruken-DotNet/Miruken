namespace ExampleTests.MirukenCastleExamplesTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Example.MirukenCastleExamples;

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
