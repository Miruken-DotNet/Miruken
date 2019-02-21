namespace Miruken.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MaybeTests
    {
        [TestMethod]
        public void Linq_Works_With_Maybe()
        {
            var result = from a in 4.Some()
                         from b in 6.Some()
                         select a + b;

            Assert.AreEqual(10, result.Value);
        }

        [TestMethod]
        public void Linq_Works_With_Nothing()
        {
            var result = from a in 4.Some()
                         from b in Maybe<int>.Nothing
                         select a + b;

            Assert.AreSame(Maybe<int>.Nothing, result);
        }
    }
}
