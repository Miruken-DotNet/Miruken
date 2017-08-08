namespace ExampleTests.MirukenCastleExamplesTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Example.MirukenCastleExamples;

    [TestClass]
    public class FeaturesFromAssembliesTests
    {
        [TestMethod]
        public void CanConfigureContainer()
        {
            var container = new FeaturesFromAssemblies().Container;
            Assert.IsNotNull(container);
        }
    }
}
