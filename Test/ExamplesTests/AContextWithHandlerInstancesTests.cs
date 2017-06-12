namespace ExamplesTests
{
    using Examples.MirukenExamples;
    using Examples.MirukenExamples.Context;
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
