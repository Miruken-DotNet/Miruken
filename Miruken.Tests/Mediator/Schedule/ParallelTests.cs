namespace Miruken.Tests.Mediator.Schedule
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Mediator;
    using Miruken.Mediator.Schedule;
    using Parallel = Miruken.Mediator.Schedule.Parallel;

    [TestClass]
    public class ParallelTests
    {
        [TestMethod]
        public async Task Should_Execute_In_Parallel()
        {
            var handler = new StockQuoteHandler()
                        + new ScheduleHandler();
            var result  = await handler.Send(new Parallel
            {
                Requests = new[]
                {
                    new GetStockQuote("AAPL"),
                    new GetStockQuote("MSFT"),
                    new GetStockQuote("GOOGL")
                }
            });

            CollectionAssert.AreEquivalent(
                new[] {"AAPL", "MSFT", "GOOGL"},
                result.Responses.Cast<StockQuote>().Select(q => q.Symbol)
                    .ToArray());
        }

        [TestMethod]
        public async Task Should_Propogate_Exception()
        {
            var handler = new StockQuoteHandler()
                        + new ScheduleHandler();
            try
            {
                await handler.Send(new Parallel
                {
                    Requests = new[]
                    {
                        new GetStockQuote("AAPL"),
                        new GetStockQuote("EX"),
                        new GetStockQuote("GOOGL")
                    }
                });
                Assert.Fail("Expected an exception");
            }
            catch (AggregateException ex)
            {
                Assert.AreEqual("Stock Exchange is down", ex.InnerException?.Message);
            }
        }
    }
}
