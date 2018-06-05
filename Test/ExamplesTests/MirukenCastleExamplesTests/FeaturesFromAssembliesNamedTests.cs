namespace ExampleTests.MirukenCastleExamplesTests
{
    using Example.mirukenCastleExamples;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
