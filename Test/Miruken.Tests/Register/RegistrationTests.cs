﻿// ReSharper disable ClassNeverInstantiated.Local
namespace Miruken.Tests.Register
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Register;

    [TestClass]
    public class RegistrationTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _handler = new ServiceCollection()
                .AddTransient<IService>(sp => new Service())
                .AddMiruken(scan =>
                    scan.PublicSources(sources => sources.FromCallingAssembly())
                );
        }

        [TestMethod]
        public void Should_Register_Handlers()
        {
            var action = new Action();
            Assert.IsTrue(_handler.Handle(action));
            Assert.AreEqual(1, action.Handled);
        }

        [TestMethod]
        public void Should_Register_Handlers_As_Singleton_By_Default()
        {
            var handler = _handler.Resolve<PrivateHandler>();
            Assert.IsNotNull(handler);
            Assert.AreSame(handler, _handler.Resolve<PrivateHandler>());
        }

        [TestMethod]
        public void Should_Register_Service_Factory()
        {
            var service = _handler.Resolve<IService>();
        }

        public class Action
        {
            public int Handled { get; set; }
        }

        public class PrivateHandler : Handler
        {
            [Handles]
            public void Process(Action action)
            {
                ++action.Handled;
            }
        }

        public interface IService
        {
            void DoSomething();
        }

        public class Service : IService
        {
            public void DoSomething()
            {
            }
        }
    }
}
