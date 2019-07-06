namespace Miruken.Tests.Register
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Register;

    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _handler = new ServiceCollection().AddMiruken(registration =>
                registration.FromPublic(scan => scan.FromCallingAssembly())
            );
        }

        [TestMethod]
        public void Should_Register_Handlers()
        {
            var foo = new Foo();
            Assert.IsTrue(_handler.Handle(foo));
            Assert.AreEqual(1, foo.Handled);
        }

        public class Foo
        {
            public int Handled { get; set; }
        }

        public class CustomHandler : Handler
        {
            [Handles]
            public void HandleFoo(Foo foo)
            {
                ++foo.Handled;
            }
        }
    }
}
