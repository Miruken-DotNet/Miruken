namespace Miruken.Tests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EitherTests
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

        [TestMethod]
        public void Linq_Either_Propagates_Left()
        {
            var result =
                from e1 in new Either<string, int>("error")
                from e2 in new Either<string, int>(28)
                select e1 + e2;

            Assert.AreEqual("error", result.Left);
        }

        [TestMethod]
        public void Linq_Works_With_Try()
        {
            var result =
                from e1 in new Either<Exception, string>("red")
                from e2 in new Either<Exception, string>("blue")
                select $"{e1} {e2}";

            Assert.AreEqual("red blue", result.Right);
        }

        [TestMethod]
        public void Linq_Try_Propagates_Exception()
        {
            var result =
                from e1 in new Either<Exception, string>("apple")
                from e2 in new Either<Exception, string>(new Exception("Broken"))
                select e1 + e2;

            Assert.AreEqual("Broken", result.Left.Message);
        }
    }
}
