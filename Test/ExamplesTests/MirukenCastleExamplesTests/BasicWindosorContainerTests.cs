namespace ExampleTests.MirukenCastleExamplesTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Example.MirukenCastleExamples;

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
