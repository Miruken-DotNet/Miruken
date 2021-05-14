namespace Miruken.Tests.Api.Cache
{
    using System;
    using System.Threading.Tasks;
    using Api;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Api.Cache;
    using Miruken.Callback;
    using Miruken.Callback.Policy;

    [TestClass]
    public class CacheHandlerTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<StockQuoteHandler>();
            factory.RegisterDescriptor<CachedHandler>();
            HandlerDescriptorFactory.UseFactory(factory);

            _handler = new StockQuoteHandler()
                     + new CachedHandler();

            StockQuoteHandler.Called = 0;
        }

        [TestMethod]
        public async Task Should_Make_Initial_Request()
        {
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            var quote    = await _handler.Send(getQuote.Cached());
            Assert.IsNotNull(quote);
            Assert.AreEqual(1, StockQuoteHandler.Called);
        }

        [TestMethod]
        public async Task Should_Cache_Initial_Response()
        {
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            var quote1   = await _handler.Send(getQuote.Cached());
            var quote2   = await _handler.Send(getQuote.Cached());
            Assert.AreEqual(1, StockQuoteHandler.Called);
            Assert.AreEqual(quote1.Value, quote2.Value);
        }

        [TestMethod]
        public async Task Should_Refresh_Response()
        {
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            var quote1 = await _handler.Send(getQuote.Cached());
            var quote2 = await _handler.Send(getQuote.Cached());
            var quote3 = await _handler.Send(getQuote.Refresh());
            Assert.AreEqual(2, StockQuoteHandler.Called);
            Assert.AreEqual(quote1.Value, quote2.Value);
            Assert.AreEqual(quote1.Symbol, quote3.Symbol);
        }

        [TestMethod]
        public async Task Should_Refresh_Stale_Response()
        {
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            await _handler.Send(getQuote.Cached());
            await Task.Delay(TimeSpan.FromSeconds(.2));
            await _handler.Send(getQuote.Cached(TimeSpan.FromSeconds(.1)));
            Assert.AreEqual(2, StockQuoteHandler.Called);
        }

        [TestMethod]
        public async Task Should_Invalidate_Response()
        {
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            var quote1   = await _handler.Send(getQuote.Cached());
            var quote2   = await _handler.Send(getQuote.Cached());
            var quote3   = await _handler.Send(getQuote.Invalidate());
            var quote4   = await _handler.Send(getQuote.Cached());
            Assert.AreEqual(2, StockQuoteHandler.Called);
            Assert.AreEqual(quote1.Value, quote2.Value);
            Assert.AreEqual(quote1.Value, quote3.Value);
            Assert.AreEqual(quote1.Value, quote3.Value);
            Assert.IsNotNull(quote4);
        }

        [TestMethod]
        public async Task Should_Not_Cache_Exceptions()
        {
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("EX");
            try
            {
                await _handler.Send(getQuote.Cached());
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Stock Exchange is down.", ex.Message);
            }
            try
            {
                await _handler.Send(getQuote.Cached());
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Stock Exchange is down.", ex.Message);
            }
            Assert.AreEqual(2, StockQuoteHandler.Called);
        }
    }
}
