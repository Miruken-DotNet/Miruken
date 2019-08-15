// ReSharper disable ClassNeverInstantiated.Local
namespace Miruken.Tests.Register
{
    using System.Collections.Generic;
    using System.Linq;
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
                .AddMiruken(scan =>
                    scan.PublicSources(sources => sources.FromAssemblyOf<RegistrationTests>())
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
        public void Should_Register_Instances()
        {
            var service = new Service1();
            var handler = new ServiceCollection()
                .AddSingleton(service)
                .AddMiruken();
            Assert.AreSame(service, handler.Resolve<IService>());
        }

        [TestMethod]
        public void Should_Register_Transient_Service_Factory()
        {
            var handler = new ServiceCollection()
                .AddTransient(sp => new Service1())
                .AddMiruken();
            var service = handler.Resolve<IService>();
            Assert.IsNotNull(service);
            Assert.AreNotSame(service, handler.Resolve<IService>());
            Assert.IsNotNull(handler.Resolve<Service1>());
        }

        [TestMethod]
        public void Should_Register_Singleton_Service_Factory()
        {
            var handler = new ServiceCollection()
                .AddSingleton(sp => new Service1())
                .AddMiruken();
            var service = handler.Resolve<IService>();
            Assert.IsNotNull(service);
            Assert.AreSame(service, handler.Resolve<IService>());
            Assert.AreSame(service, handler.Resolve<Service1>());
        }

        [TestMethod]
        public void Should_Register_Scoped_Service_Factory()
        {
            var handler = new ServiceCollection()
                .AddScoped(sp => new Service1())
                .AddMiruken();
            var service = handler.Resolve<IService>();
            Assert.IsNotNull(service);
            Assert.AreSame(service, handler.Resolve<IService>());
            Assert.AreSame(service, handler.GetService<IService>());
            using (var scope = handler.CreateScope())
            {
                Assert.AreNotSame(service, scope.ServiceProvider.GetService<IService>());
                Assert.AreSame(service, handler.Resolve<IService>());
            }
        }

        [TestMethod]
        public void Should_Handle_Composition()
        {
            var service = new Service1();
            var handler = new ServiceCollection()
                .AddSingleton(service)
                .AddTransient<Service2>()
                .AddTransient<CompositeService>()
                .AddMiruken();
            var c = handler.Resolve<CompositeService>();
            Assert.AreEqual(2, c.Services.Count());
            Assert.IsTrue(c.Services.Contains(service));
            Assert.IsTrue(c.Services.OfType<Service2>().Any());
        }

        [TestMethod]
        public void Should_Handle_Composition_With_Factories()
        {
            var service = new Service1();
            var handler = new ServiceCollection()
                .AddSingleton(service)
                .AddTransient<Service2>()
                .AddTransient(sp =>
                {
                    var s = sp.GetService<IEnumerable<IService>>();
                    return new CompositeService(s);
                })
                .AddMiruken();
            var c = handler.Resolve<CompositeService>();
            Assert.AreEqual(2, c.Services.Count());
            Assert.IsTrue(c.Services.Contains(service));
            Assert.IsTrue(c.Services.OfType<Service2>().Any());
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

        public class Service1 : IService
        {
            public void DoSomething()
            {
            }
        }

        public class Service2 : IService
        {
            public void DoSomething()
            {
            }
        }

        public class CompositeService : IService
        {
            public IEnumerable<IService> Services { get; }

            public CompositeService(IEnumerable<IService> services)
            {
                Services = services;
            }

            public void DoSomething()
            {
                foreach (var service in Services)
                    service.DoSomething();
            }
        }
    }
}
