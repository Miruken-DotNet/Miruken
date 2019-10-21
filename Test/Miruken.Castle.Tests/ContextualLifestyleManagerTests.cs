namespace Miruken.Castle.Tests
{
    using System;
    using Callback;
    using Callback.Policy;
    using Context;
    using global::Castle.MicroKernel;
    using global::Castle.MicroKernel.Registration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ContextualLifestyleManagerTests
    {
        protected Context _rootContext;
        protected WindsorHandler _container;

        public class Controller : Contextual, IDisposable
        {    
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _rootContext = new Context();
            _container   = new WindsorHandler();
            _rootContext.AddHandlers(_container);
            _container.Container.Register(Component.For<Controller>()
                .LifestyleCustom<ContextualLifestyleManager>());

            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<WindsorHandler>();
            factory.RegisterDescriptor<Controller>();
            HandlerDescriptorFactory.UseFactory(factory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
            _rootContext.End();
        }

        [TestMethod]
        public void Should_Resolve_Same_Instance_Per_Context()
        {
            var controller1 = _rootContext.Resolve<Controller>();
            var controller2 = _rootContext.Resolve<Controller>();
            Assert.IsInstanceOfType(controller1, typeof(Controller));
            Assert.AreSame(_rootContext, controller1.Context);
            Assert.AreSame(controller1, controller2);
        }

        [TestMethod]
        public void Should_Resolve_All_Per_Context()
        {
            var controllers1 = _rootContext.ResolveAll<Controller>();
            var controllers2 = _rootContext.ResolveAll<Controller>();
            Assert.AreEqual(1, controllers1.Length);
            CollectionAssert.AreEqual(controllers1, controllers2);
        }

        [TestMethod]
        public void Should_Resolve_Different_Instance_Per_Context()
        {
            using (var child = _rootContext.CreateChild())
            {
                var controller1 = _rootContext.Resolve<Controller>();
                var controller2 = child.Resolve<Controller>();
                Assert.IsInstanceOfType(controller1, typeof(Controller));
                Assert.IsInstanceOfType(controller2, typeof(Controller));
                Assert.AreSame(_rootContext, controller1.Context);
                Assert.AreNotSame(controller1, controller2);
                Assert.AreSame(child, controller2.Context);
            }
        }

        [TestMethod]
        public void Should_Release_Components_When_Context_Ends()
        {
            var child      = _rootContext.CreateChild();
            var controller = child.Resolve<Controller>();
            Assert.AreSame(child, controller.Context);
            Assert.IsFalse(controller.Disposed);
            child.End();
            Assert.IsTrue(controller.Disposed);
            Assert.IsNull(controller.Context);
        }

        [TestMethod]
        public void Should_Release_Component_When_Context_Cleared()
        {
            var child      = _rootContext.CreateChild();
            var controller = child.Resolve<Controller>();
            Assert.AreSame(child, controller.Context);
            Assert.IsFalse(controller.Disposed);
            controller.Context = null;
            Assert.IsTrue(controller.Disposed);
            Assert.IsNull(controller.Context);
        }

        [TestMethod]
        public void Should_Resolve_Different_Instance_Same_Context_When_Cleared()
        {
            var controller1 = _rootContext.Resolve<Controller>();
            controller1.Context = null;
            var controller2 = _rootContext.Resolve<Controller>();
            Assert.IsInstanceOfType(controller1, typeof(Controller));
            Assert.AreSame(_rootContext, controller2.Context);
            Assert.AreNotSame(controller1, controller2);
        }

        [TestMethod, ExpectedException(typeof(ComponentResolutionException))]
        public void Should_Fail_If_Context_Not_Available()
        {
            _container.Resolve<Controller>();
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException),
            "Container managed instances cannot change context")]
        public void Should_Reject_Changing_Context()
        {
            var controller = _rootContext.Resolve<Controller>();
            controller.Context = _rootContext.CreateChild();
        }
    }
}
