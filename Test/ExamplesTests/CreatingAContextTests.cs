namespace ExampleTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Example.MirukenExamples.Context;

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
