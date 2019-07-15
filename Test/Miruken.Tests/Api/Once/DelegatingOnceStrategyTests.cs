namespace Miruken.Tests.Api.Once
{
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Api.Once;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Map;

    [TestClass]
    public class DelegatingOnceStrategyTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<FooHandler>();
            factory.RegisterDescriptor<OnceHandler>();
            factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(factory);

            _handler = new FooHandler() + new OnceHandler();
        }

        [TestMethod]
        public async Task Should_Delegate_Once()
        {
            var foo  = new Foo().Once();
            var once = await _handler.Send<Once>(foo);
            Assert.AreEqual(foo.RequestId, once.RequestId);
        }

        private class Foo { }

        private class FooHandler : Handler
        {
            [Handles]
            public Once HandleFoo(Foo foo, Once once)
            {
                return once;
            }

            [Maps]
            public IOnceStrategy Once(Foo foo)
            {
                return DelegatingOnceStrategy.Instance;
            }
        }
    }
}
