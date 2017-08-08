namespace ExampleTests.MirukenCastleExamplesTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Example.MirukenCastleExamples;

    [TestClass]
    public class FeaturesFromAssembliesNamedTests
    {
        [TestMethod]
        public void CanConfigureContainer()
        {
            var container = new FeaturesFromAssembliesNamed().Container;
            Assert.IsNotNull(container);
        }
    }
}
