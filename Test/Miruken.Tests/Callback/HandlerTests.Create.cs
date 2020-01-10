namespace Miruken.Tests.Callback
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;

    [TestClass]
    public class HandlerCreateTests
    {
        [TestInitialize]
        public void Setup()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<ViewFactory>();
            factory.RegisterDescriptor<Controller>();
            HandlerDescriptorFactory.UseFactory(factory);
        }

        [TestMethod]
        public void Should_Create_Instance()
        {
            var controller = new StaticHandler().Create<Controller>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.ViewFactory);
        }

        [TestMethod]
        public async Task Should_Create_Instance_Asynchronously()
        {
            var controller = await new StaticHandler().CreateAsync<Controller>();
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
         ExpectedException(typeof(ArgumentException))]
        public void Should_Reject_Interface_Creation()
        {
            new StaticHandler().Create<IInventory>();
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException))]
        public void Should_Reject_Abstract_Class_Creation()
        {
            new StaticHandler().Create<RepositoryBase>();
        }

        private class ViewFactory { }

        private class Controller
        {
            [Creates]
            public Controller(ViewFactory viewFactory)
            {
                ViewFactory = viewFactory;
            }

            public ViewFactory ViewFactory { get; }
        }

        private interface IInventory { }

        private abstract class RepositoryBase { }
    }
}
