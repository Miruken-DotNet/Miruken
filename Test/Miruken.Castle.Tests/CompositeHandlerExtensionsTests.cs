namespace Miruken.Castle.Tests
{
    using Callback;
    using Callback.Policy;
    using Context;
    using global::Castle.MicroKernel.Registration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CompositeHandlerExtensionsTests
    {
        protected Context _rootContext;
        protected WindsorHandler _container;

        public class MyHandler : Handler
        {
            public MyHandler(Context context)
            {
                Context = context;
            }

            public Context Context { get; }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _rootContext = new Context();
            _container   = new WindsorHandler(container => 
                container.Kernel.Resolver.AddSubResolver(new ExternalDependencyResolver()));
            _rootContext.AddHandlers(_container);
            _container.Container.Register(Component.For<MyHandler>());

            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<WindsorHandler>();
            factory.RegisterDescriptor<MyHandler>();
            HandlerDescriptorFactory.UseFactory(factory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
            _rootContext.End();
        }

        [TestMethod]
        public void Should_Resolve_Handler_From_Container()
        {
            _rootContext.AddHandlers(_rootContext.Resolve<MyHandler>());
            var handler = _rootContext.Resolve<MyHandler>();
            Assert.AreSame(_rootContext, handler.Context);
        }
    }
}
