namespace Miruken.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MonadsTests
    {
        [TestMethod]
        public void Linq_Works_With_Either()
        {
            var result =
                from e1 in new Either<string, int>(22)
                from e2 in new Either<string, int>(28)
                select e1 + e2;

            Assert.AreEqual(50, result.Right);
        }
    }
}
