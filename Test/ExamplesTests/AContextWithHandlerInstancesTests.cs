namespace ExampleTests
{
    using Example.mirukenExamples.context;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AContextWithHandlerInstancesTests
    {
        [TestMethod]
        public void CreatesContext()
        {
            var target = new AContextWithHandlerInstances();
            Assert.IsNotNull(target.Context);
            Assert.AreEqual(2, target.Context.Handlers.Length);
        }
    }
}
