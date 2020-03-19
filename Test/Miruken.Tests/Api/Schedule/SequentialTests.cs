namespace Miruken.Tests.Api.Schedule
{
    using System.Linq;
    using System.Threading.Tasks;
    using Api;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Api.Schedule;
    using Miruken.Callback.Policy;

    [TestClass]
    public class SequentialTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<StockQuoteHandler>();
            factory.RegisterDescriptor<Scheduler>();
            StockQuoteHandler.Called = 0;
        }

        [TestMethod]
        public async Task Should_Execute_Sequentially()
        {
            var handler = new StockQuoteHandler()
                        + new Scheduler();
            var result  = await handler.Send(new Sequential
            {
                Requests = new[]
                {
                    new GetStockQuote("APPL"),
                    new GetStockQuote("MSFT"),
                    new GetStockQuote("GOOGL")
                }
            });
            CollectionAssert.AreEqual(
                new[] { "APPL", "MSFT", "GOOGL" },
                result.Responses.Select(r => r.Match(
                        error => error.Message,
                        quote => ((StockQuote)quote).Symbol))
                    .ToArray());
        }

        [TestMethod]
        public async Task Should_Execute_Sequentially_Params()
        {
            var handler = new StockQuoteHandler()
                          + new Scheduler();
            var result  = await handler.Sequential(
                new GetStockQuote("APPL"),
                new GetStockQuote("MSFT"),
                new GetStockQuote("GOOGL"));
            CollectionAssert.AreEqual(
                new[] { "APPL", "MSFT", "GOOGL" },
                result.Responses.Select(r => r.Match(
                        error => error.Message,
                        quote => ((StockQuote)quote).Symbol))
                    .ToArray());
        }

        [TestMethod]
        public async Task Should_Execute_Sequentially_Enumerable()
        {
            var handler = new StockQuoteHandler()
                        + new Scheduler();
            var result  = await handler.Sequential(new []
                {
                    new GetStockQuote("APPL"),
                    new GetStockQuote("MSFT"),
                    new GetStockQuote("GOOGL")
                }.ToList());
            CollectionAssert.AreEqual(
                new[] { "APPL", "MSFT", "GOOGL" },
                result.Responses.Select(r => r.Match(
                        error => error.Message,
                        quote => ((StockQuote)quote).Symbol))
                    .ToArray());
        }

        [TestMethod]
        public async Task Should_Stop_At_First_Exception()
        {
            var handler = new StockQuoteHandler()
                        + new Scheduler();
            var result  = await handler.Send(new Sequential
            {
                Requests = new[]
                {
                    new GetStockQuote("APPL"),
                    new GetStockQuote("EX"),
                    new GetStockQuote("GOOGL")
                }
            });
            CollectionAssert.AreEqual(
                new[] { "APPL", "Stock Exchange is down" },
                result.Responses.Select(r => r.Match(
                        error => error.Message,
                        quote => ((StockQuote)quote).Symbol))
                    .ToArray());
        }

        [TestMethod]
        public async Task Should_Publish_Concurrently()
        {
            var handler = new StockQuoteHandler()
                        + new StockQuoteHandler()
                        + new Scheduler();
            var result = await handler.Send(new Sequential
            {
                Requests = new[]
                {
                    new SellStock("AAPL", 2).Publish(),
                    new SellStock("MSFT", 1).Publish(),
                    new SellStock("GOOGL", 2).Publish()
                }
            });

            Assert.AreEqual(3, result.Responses.Length);
            Assert.AreEqual(6, StockQuoteHandler.Called);
        }
    }
}
