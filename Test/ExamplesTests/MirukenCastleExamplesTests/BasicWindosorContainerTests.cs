namespace ExampleTests.MirukenCastleExamplesTests
{
    using Example.mirukenCastleExamples;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BasicWindosorContainerTests
    {
        [TestMethod]
        public void CanConfigureContainer()
        {
            var target = new BasicWindsorContainer();
            Assert.IsNotNull(target.Container);
        }
    }
}
