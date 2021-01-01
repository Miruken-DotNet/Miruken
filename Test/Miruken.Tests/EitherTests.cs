namespace Miruken.Tests
{
    using System;
    using Functional;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EitherTests
    {
        [TestMethod]
        public void Linq_Works_With_Either()
        {
            var result =
                from e1 in new Either<string, int>.Right(22)
                from e2 in new Either<string, int>.Right(28)
                select e1 + e2;

            Assert.AreEqual(50, result.RightOrDefault());
        }

        [TestMethod]
        public void Linq_Either_Propagates_Left()
        {
            var result =
                from e1 in new Either<string, int>.Left("error")
                from e2 in new Either<string, int>.Right(28)
                select e1 + e2;

            Assert.AreEqual("error", result.LeftOrDefault());
        }

        [TestMethod]
        public void Linq_Works_With_Try()
        {
            var result =
                from e1 in new Try<Exception, string>.Success("red")
                from e2 in new Try<Exception, string>.Success("blue")
                select $"{e1} {e2}";

            Assert.AreEqual("red blue", result.SuccessOrDefault());
        }

        [TestMethod]
        public void Linq_Try_Propagates_Exception()
        {
            var result =
                from e1 in new Try<Exception, string>.Success("apple")
                from e2 in new Try<Exception, string>.Failure(new Exception("Broken"))
                select e1 + e2;

            Assert.AreEqual("Broken", result.FailureOrDefault()?.Message);
        }
    }
}
