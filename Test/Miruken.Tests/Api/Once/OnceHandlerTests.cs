namespace Miruken.Tests.Api.Once
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Api;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Api.Once;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Map;

    [TestClass]
    public class OnceHandlerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            StockQuoteHandler.Called = 0;

            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<StockQuoteHandler>();
            factory.RegisterDescriptor<OnceHandler>();
            factory.RegisterDescriptor<TestOnceStrategy>();
            HandlerDescriptorFactory.UseFactory(factory);
        }

        [TestMethod]
        public async Task Should_Handle_Once()
        {
            var handler = new StockQuoteHandler()
                        + new OnceHandler()
                        + new TestOnceStrategy();
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL").Once();
            await handler.Send(getQuote);
            Assert.AreEqual(1, StockQuoteHandler.Called);
            await handler.Send(getQuote);
            Assert.AreEqual(1, StockQuoteHandler.Called);
            await handler.Send(new GetStockQuote("AAPL"));
            Assert.AreEqual(2, StockQuoteHandler.Called);
        }

        public class TestOnceStrategy : Handler, IOnceStrategy
        {
            private readonly HashSet<Guid> _requests = new();

            [Maps]
            public IOnceStrategy Once(GetStockQuote request)
            {
                return this;
            }

            public async Task Complete(Once once, IHandler composer)
            {
                if (!_requests.Contains(once.RequestId))
                {
                    await composer.Send(once.Request);
                    _requests.Add(once.RequestId);
                }
            }
        }
    }
}
