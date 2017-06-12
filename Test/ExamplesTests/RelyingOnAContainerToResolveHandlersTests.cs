namespace ExamplesTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Examples.MirukenExamples.Context;

    [TestClass]
    public class RelyingOnAContainerToResolveHandlersTests
    {
        [TestMethod]
        public void ResolvesHandlersFromContainer()
        {
            var target = new RelyingOnAContainerToResolveHandlers();
            Assert.IsNotNull(target.Context);
            Assert.AreEqual(3, target.Context.Handlers.Length);
        }
    }
}
