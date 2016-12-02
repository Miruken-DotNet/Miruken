using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Miruken.Infrastructure;

namespace SixFlags.CF.Miruken.Tests
{
    [TestClass]
    public class DisposableObjectTests
    {
        [TestMethod]
        public void ShouldDisposeIfNotNull()
        {
            //Arrange
            var d = MockRepository.GenerateMock<IDisposable>();
            d.Expect(x => x.Dispose()).Repeat.Once();

            //Act
            d.TryDispose();

            //Assert
            d.VerifyAllExpectations();
        }

        [TestMethod]
        public void ShouldReturnExceptionIfDisposeThrows()
        {
            //Arrange
            var d = MockRepository.GenerateMock<IDisposable>();
            d.Expect(x => x.Dispose()).Throw(new Exception("Problem with Dispose")).Repeat.Once();

            //Act
            var ex = d.TryDispose();

            //Assert
            Assert.IsNotNull(ex);
            d.VerifyAllExpectations();
        }

        [TestMethod]
        public void ShouldNotThrowErrorIfNull()
        {
            //Arrange
            IDisposable d = null;

            //Act
            var ex = d.TryDispose();

            //Assert
            Assert.IsNull(ex);
        }

        [TestMethod]
        public void ShouldFireDisposedEvent()
        {
            ///Arrange
            var count = 0;
            var d = MockRepository.GeneratePartialMock<DisposableObject>();
            d.Disposed += (o, e) => count++;

            //Act
            d.Dispose();

            //Assert
            Assert.AreEqual(1, count);
        }
    }
}
