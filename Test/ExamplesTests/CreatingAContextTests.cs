namespace ExampleTests
{
    using Example.mirukenExamples.context;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
