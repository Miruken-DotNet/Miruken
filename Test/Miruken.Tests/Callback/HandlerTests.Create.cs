namespace Miruken.Tests.Callback
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Concurrency;
    using Miruken.Infrastructure;

    [TestClass]
    public class HandlerCreateTests
    {
        [TestInitialize]
        public void Setup()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<ViewFactory>();
            factory.RegisterDescriptor<Controller>();
            factory.RegisterDescriptor<Warehouse>();
            HandlerDescriptorFactory.UseFactory(factory);
        }

        [TestMethod]
        public void Should_Create_Instance()
        {
            var controller = new StaticHandler().Create<Controller>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.ViewFactory);
            Assert.IsTrue(controller.Initialized);
        }

        [TestMethod]
        public void Should_Create_Instance_From_Interface()
        {
            var controller = new StaticHandler().Create<IController>();
            Assert.IsNotNull(controller);
            Assert.IsInstanceOfType(controller, typeof(Controller));
            Assert.IsNotNull((controller as Controller)?.ViewFactory);
        }

        [TestMethod]
        public async Task Should_Create_Instance_Asynchronously()
        {
            var controller = await new StaticHandler().CreateAsync<Controller>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.ViewFactory);
        }

        [TestMethod]
        public void Should_Create_All_Instances()
        {
            var controllers = new StaticHandler().CreateAll<Controller>();
            Assert.AreEqual(1, controllers.Length);
            Assert.IsNotNull(controllers[0]);
        }

        [TestMethod]
        public async Task Should_Create_All_Instances_Asynchronously()
        {
            var controllers = await new StaticHandler().CreateAllAsync<Controller>();
            Assert.AreEqual(1, controllers.Length);
            Assert.IsNotNull(controllers[0]);
        }

        [TestMethod]
        public void Should_Resolve_Instance()
        {
            var controller = new StaticHandler().Resolve<Controller>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.ViewFactory);
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Reject_Unhandled_Creation()
        {
            new StaticHandler().Create<ViewFactory>();
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public async Task Should_Reject_Unhandled_Creation_Async()
        {
            await new StaticHandler().CreateAsync<ViewFactory>();
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Reject_Creation_If_Missing_Dependencies()
        {
            new StaticHandler().Create<Warehouse>();
        }

        private class ViewFactory { }

        private interface IController { }

        private class Controller : IController, IInitialize
        {
            public bool Initialized { get; set; }

            public Promise Initialize()
            {
                Initialized = true;
                return null;
            }

            public void FailedInitialize(Exception exception = null)
            {
            }

            [Creates]
            public Controller(ViewFactory viewFactory)
            {
                ViewFactory = viewFactory;
            }

            public ViewFactory ViewFactory { get; }
        }

        private interface IDelivery { }

        private class Warehouse
        {
            [Creates]
            public Warehouse(IDelivery delivery)
            {
                Delivery = delivery;
            }

            public IDelivery Delivery { get; }
        }
    }
}
