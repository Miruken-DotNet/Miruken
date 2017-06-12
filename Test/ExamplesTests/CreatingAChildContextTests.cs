namespace ExamplesTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Examples.MirukenExamples.Context;

    [TestClass]
    public class CreatingAChildContextTests
    {
        [TestMethod]
        public void CreatesAChildContext()
        {
            var target = new CreatingAChildContext();
            Assert.IsNotNull(target.Parent);
            Assert.IsNotNull(target.Child);
        }
    }
}
