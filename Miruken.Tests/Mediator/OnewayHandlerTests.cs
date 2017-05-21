namespace Miruken.Tests.Mediator
{
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Mediator;

    [TestClass]
    public class OnewayHandlerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            StockQuoteHandler.Called = 0;
        }

        [TestMethod]
        public async Task Should_Ignore_Response()
        {
            var handler = new StockQuoteHandler()
                        + new OnewayHandler();
            Assert.AreEqual(0, StockQuoteHandler.Called);
            var getQuote = new GetStockQuote("AAPL");
            await handler.Send(getQuote.Oneway());
            Assert.AreEqual(1, StockQuoteHandler.Called);
        }
    }
}
