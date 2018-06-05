namespace ExampleTests.MirukenCastleExamplesTests
{
    using Example.mirukenCastleExamples;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
