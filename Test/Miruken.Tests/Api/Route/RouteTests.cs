namespace Miruken.Tests.Api.Route
{
    using System;
    using System.Threading.Tasks;
    using Api;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Api.Route;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Concurrency;

    [TestClass]
    public class RouteTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            HandlerDescriptor.GetDescriptor<StockQuoteHandler>();
            HandlerDescriptor.GetDescriptor<PassThroughRouter>();
            HandlerDescriptor.GetDescriptor<TrashHandler>();

            _handler = new StockQuoteHandler()
                     + new PassThroughRouter()
                     + new TrashHandler();
            StockQuoteHandler.Called = 0;
        }

        [TestMethod]
        public async Task Should_Route_Requests()
        {
            var quote = await _handler.Send(new GetStockQuote("GOOGL").RouteTo("Trash"));
            Assert.IsNull(quote);
        }

        [TestMethod]
        public async Task Should_Route_Requests_With_No_Responses()
        {
            await _handler.Send(new Pickup().RouteTo("Trash"));
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public async Task Should_Fail_For_Unrecognized_Routes()
        {
            var handler = new StockQuoteHandler()
                        + new PassThroughRouter();
            await handler.Send(new Pickup().RouteTo("NoWhere"));
        }

        [TestMethod]
        public async Task Should_Route_Request_Through_Pipeline()
        {
            var quote = await _handler.Send(new GetStockQuote("MSFT")
                .RouteTo(PassThroughRouter.Scheme));
            Assert.AreSame("MSFT", quote.Symbol);
        }

        public class Pickup
        {
        }

        public class TrashHandler : Handler
        {
            public const string Scheme = "Trash";

            [Handles]
            public Promise Route(Routed request, IHandler composer)
            {
                return request.Route == Scheme ? Promise.Empty : null;
            }
        }

        private class PassThroughRouter : Handler
        {
            public const string Scheme = "pass-through";

            [Handles]
            public Promise Route(Routed request, IHandler composer)
            {
                return request.Route == Scheme
                    ? composer.Send(request.Message)
                    : null;
            }
        }
    }
}
