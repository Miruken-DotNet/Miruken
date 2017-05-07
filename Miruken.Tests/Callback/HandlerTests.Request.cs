namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Concurrency;

    [TestClass]
    public class HandlerRequestTests
    {
        [TestMethod]
        public void Should_Request_With_Response()
        {
            var handler = new OrderHandler();
            var order   = new PlaceOrder();
            var orderId = handler.Request<int>(order);
            Assert.AreEqual(1, orderId);
        }

        [TestMethod]
        public void Should_Request_Without_Response()
        {
            var handler = new OrderHandler();
            var change  = new CancelOrder {OrderId = 1};
            handler.Request(change);
            Assert.AreEqual(1, change.OrderId);
        }

        [TestMethod]
        public async Task Should_Request_Asynchronously()
        {
            var handler = new OrderHandler();
            var fulfill = new FulfillOrder {OrderId = 1};
            await handler.RequestAsync(fulfill);
            Assert.AreEqual(1, fulfill.OrderId);
        }

        [TestMethod]
        public async Task Should_Request_With_Response_Asynchronously()
        {
            var handler  = new OrderHandler();
            var deliver  = new DeliverOrder { OrderId = 1 };
            var tracking = await handler.RequestAsync<Guid>(deliver);
            Assert.AreNotEqual(Guid.Empty, tracking);
            Assert.AreEqual(1, deliver.OrderId);
        }

        [TestMethod]
        public async Task Should_Request_All_Asycnhronously()
        {
            var handler   = new OrderHandler();
            var fulfill   = new FulfillOrder { OrderId = 1 };
            var results   = await handler.RequestAllAsync(fulfill);
            var responses = ((IEnumerable)results).Cast<bool>().ToArray();
            Assert.AreEqual(2, responses.Length);
            Assert.IsTrue(responses.All(b => b));
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException),
            "Miruken.Tests.Callback.HandlerRequestTests+UpdateOrder not handled")]
        public void Should_Reject_Unhandled_Request()
        {
            var handler = new OrderHandler();
            handler.Request(new UpdateOrder());
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException),
            "Miruken.Tests.Callback.HandlerRequestTests+UpdateOrder not handled")]
        public async Task Should_Reject_Unhandled_Request_Async()
        {
            var handler = new OrderHandler();
            await handler.RequestAsync(new UpdateOrder());
        }

        private class PlaceOrder
        {
        }

        private class UpdateOrder
        {
        }

        private class CancelOrder
        {
            public int OrderId { get; set; }
        }

        private class FulfillOrder
        {    
            public int OrderId { get; set; }
        }

        private class DeliverOrder
        {
            public int OrderId { get; set; }
        }

        private class OrderHandler : Handler
        {
            private int _nextOrderId;

            [Handles]
            public int Place(PlaceOrder order)
            {
                return ++_nextOrderId;
            }

            [Handles]
            public void Cancel(CancelOrder cancel)
            {
            }

            [Handles]
            public Promise Fulfill(FulfillOrder fulfill)
            {
                return new Promise<bool>((resolve, reject) => resolve(true, true));
            }

            [Handles]
            public Promise<Guid> Deliver(DeliverOrder deliver)
            {
                return new Promise<Guid>((resolve, reject) => 
                    resolve(Guid.NewGuid(), true));
            }

            [Handles]
            public Promise Process(Request request, IHandler composer)
            {
                var callback = request.Callback;
                if (callback is FulfillOrder)
                    return new Promise<bool>((resolve, reject) => resolve(true, true));
                return null;
            }
        }
    }
}
