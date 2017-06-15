namespace ExampleTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Example.MirukenExamples.Context;

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
