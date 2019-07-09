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
    using Miruken.Concurrency;

    [TestClass]
    public class OnceHandlerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            StockQuoteHandler.Called = 0;

            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<StockQuoteHandler>();
            factory.RegisterDescriptor<TestOnceHandler>();
            HandlerDescriptorFactory.UseFactory(factory);
        }

        [TestMethod]
        public async Task Should_Handle_Once()
        {
            var handler = new StockQuoteHandler()
                        + new TestOnceHandler();
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL").Once();
            await handler.Send(getQuote);
            Assert.AreEqual(1, StockQuoteHandler.Called);
            await handler.Send(getQuote);
            Assert.AreEqual(1, StockQuoteHandler.Called);
            await handler.Send(new GetStockQuote("AAPL"));
            Assert.AreEqual(2, StockQuoteHandler.Called);
        }

        public class TestOnceHandler : OnceHandler
        {
            private readonly HashSet<Guid> _requests = new HashSet<Guid>();

            [Provides, Singleton]
            public TestOnceHandler()
            {            
            }

            protected override Promise Handle(Once once, IHandler composer)
            {
                if (_requests.Contains(once.RequestId))
                    return Promise.Empty;
                return composer.Send(once).Then((result, _) =>
                    _requests.Add(once.RequestId));
            }
        }
    }
}
