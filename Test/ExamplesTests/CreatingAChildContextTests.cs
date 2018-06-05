namespace ExampleTests
{
    using Example.mirukenExamples.context;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
