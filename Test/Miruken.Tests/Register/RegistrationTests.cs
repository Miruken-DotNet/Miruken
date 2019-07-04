// ReSharper disable ClassNeverInstantiated.Local
namespace Miruken.Tests.Register
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Register;

    [TestClass]
    public class RegistrationTests
    {
        private IHandlerDescriptorFactory _factory;
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _factory = new Registration()
                .From(scan => scan.FromCallingAssembly())
                .Exclude(type => type.Name.Contains("Bad"))
                .Register();

            _handler = new StaticHandler().Infer();
        }

        [TestMethod]
        public void Should_Register_Handlers()
        {
            Assert.IsNotNull(_factory.GetDescriptor<PrivateHandler>());

            Assert.IsTrue(_handler.Handle(new Action()));
        }

        public class Action { }

        private class PrivateHandler : Handler
        {
            [Singleton]
            public PrivateHandler()
            {
                
            }

            [Handles]
            public void Process(Action action)
            {
            }
        }
    }
}
