namespace ExamplesTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Examples.MirukenExamples.Context;

    [TestClass]
    public class CreatingAContextTests
    {
        [TestMethod]
        public void CreatesAContext()
        {
            var target = new CreatingAContext();
            Assert.IsNotNull(target.Context);
        }
    }
}
