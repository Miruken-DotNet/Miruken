using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Infrastructure;
using System.IO.Ports;

namespace SixFlags.CF.Miruken.Tests
{
    [TestClass]
    public class EnumHelperTests
    {
        [TestMethod]
        public void ShouldParseEnum()
        {
            //Arrange
            var expected = Parity.Even;
            var defaultP = Parity.None;
            var data = expected.ToString();

            //Act
            Parity actual;
            var result = EnumHelper.TryParse(data, defaultP, out actual);

            //Assert
            Assert.IsTrue(result);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ShouldNotParseBadString()
        {
            //Arrange
            var defaultP = Parity.None;
            var data = "Bob";

            //Act
            Parity actual;
            var result = EnumHelper.TryParse(data, defaultP, out actual);

            //Assert
            Assert.IsFalse(result);
            Assert.AreEqual(defaultP, actual);
        }

        [TestMethod]
        public void ShouldNotParseEnum()
        {
            //Arrange
            var defaultP = Parity.None;
            var data = "9";

            //Act
            Parity actual;
            var result = EnumHelper.TryParse(data, defaultP, out actual);

            //Assert
            Assert.IsFalse(result);
            Assert.AreEqual(defaultP, actual);
        }
    }
}
