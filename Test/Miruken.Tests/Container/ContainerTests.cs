using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Miruken.Tests.Container
{
    using System.Threading.Tasks;
    using Miruken.Callback;
    using Miruken.Container;
    using Miruken.Context;

    [TestClass]
    public class ContainerTests
    {
        protected Context _rootContext;
        protected TestContainer _container;

        public class MyHandler : Handler
        {
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _rootContext = new Context();
            _container   = new TestContainer();
            _rootContext.AddHandlers(_container);
        }


        [TestCleanup]
        public void TestCleanup()
        {
            _rootContext.End();
        }

        [TestMethod]
        public void Should_Resolve_Handler_From_Container()
        {
            _rootContext.AddHandler<MyHandler>();
            var handler = _rootContext.Resolve<MyHandler>();
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public async Task Should_Resolve_Handler_From_Container_Async()
        {
            _rootContext.AddHandler<MyHandler>();
            var handler = await _rootContext.ResolveAsync<MyHandler>();
            Assert.IsNotNull(handler);
        }
    }
}
