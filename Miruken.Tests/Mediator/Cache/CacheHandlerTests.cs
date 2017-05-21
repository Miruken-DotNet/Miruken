namespace Miruken.Tests.Mediator.Cache
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Mediator;
    using Miruken.Mediator.Cache;

    [TestClass]
    public class CacheHandlerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            StockQuoteHandler.Called = 0;
        }

        [TestMethod]
        public async Task Should_Make_Initial_Request()
        {
            var handler  = new StockQuoteHandler()
                         + new CachedHandler();
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            var quote    = await handler.Send(getQuote.Cached());
            Assert.IsNotNull(quote);
            Assert.AreEqual(1, StockQuoteHandler.Called);
        }

        [TestMethod]
        public async Task Should_Cache_Initial_Response()
        {
            var handler  = new StockQuoteHandler()
                         + new CachedHandler();
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            var quote1   = await handler.Send(getQuote.Cached());
            var quote2   = await handler.Send(getQuote.Cached());
            Assert.AreEqual(1, StockQuoteHandler.Called);
            Assert.AreEqual(quote1.Value, quote2.Value);
        }

        [TestMethod]
        public async Task Should_Refresh_Stale_Response()
        {
            var handler  = new StockQuoteHandler()
                         + new CachedHandler();
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            await handler.Send(getQuote.Cached());
            await Task.Delay(TimeSpan.FromSeconds(.2));
            await handler.Send(getQuote.Cached(TimeSpan.FromSeconds(.1)));
            Assert.AreEqual(2, StockQuoteHandler.Called);
        }

        [TestMethod]
        public async Task Should_Invalidate_Cache()
        {
            var handler  = new StockQuoteHandler()
                         + new CachedHandler();
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            var quote1   = await handler.Send(getQuote.Cached());
            var quote2   = await handler.Send(getQuote.Cached());
            var quote3   = await handler.Send(getQuote.Invalidate());
            var quote4   = await handler.Send(getQuote.Cached());
            Assert.AreEqual(2, StockQuoteHandler.Called);
            Assert.AreEqual(quote1.Value, quote2.Value);
            Assert.AreEqual(quote1.Value, quote3.Value);
            Assert.AreEqual(quote1.Value, quote3.Value);
            Assert.IsNotNull(quote4);
        }

        [TestMethod]
        public async Task Should_Not_Cache_Exceptions()
        {
            var handler = new StockQuoteHandler()
                        + new CachedHandler();
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("EX");
            try
            {
                await handler.Send(getQuote.Cached());
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Stock Exchange is down", ex.Message);
            }
            try
            {
                await handler.Send(getQuote.Cached());
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Stock Exchange is down", ex.Message);
            }
            Assert.AreEqual(2, StockQuoteHandler.Called);
        }
    }
}
