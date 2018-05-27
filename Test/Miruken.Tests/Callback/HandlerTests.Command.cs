namespace Miruken.Tests.Callback
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Concurrency;

    [TestClass]
    public class HandlerCommandTests
    {
        [TestMethod]
        public void Should_Command_With_Result()
        {
            var handler = new OrderHandler();
            var order   = new PlaceOrder();
            var orderId = handler.Command<int>(order);
            Assert.AreEqual(1, orderId);
        }

        [TestMethod]
        public void Should_Command_Without_Result()
        {
            var handler = new OrderHandler();
            var change  = new CancelOrder {OrderId = 1};
            handler.Command(change);
            Assert.AreEqual(1, change.OrderId);
        }

        [TestMethod]
        public async Task Should_Command_Asynchronously()
        {
            var handler = new OrderHandler();
            var fulfill = new FulfillOrder {OrderId = 1};
            await handler.CommandAsync(fulfill);
            Assert.AreEqual(1, fulfill.OrderId);
        }

        [TestMethod]
        public async Task Should_Command_With_Result_Asynchronously()
        {
            var handler  = new OrderHandler();
            var deliver  = new DeliverOrder { OrderId = 1 };
            var tracking = await handler.CommandAsync<Guid>(deliver);
            Assert.AreNotEqual(Guid.Empty, tracking);
            Assert.AreEqual(1, deliver.OrderId);
        }

        [TestMethod]
        public void Should_Command_All()
        {
            var handler   = new OrderHandler();
            var fulfill   = new FulfillOrder { OrderId = 1 };
            var results   = handler.CommandAll<bool>(fulfill);
            var responses = results.ToArray();
            Assert.AreEqual(2, responses.Length);
            Assert.IsTrue(responses.All(b => b));
        }

        [TestMethod]
        public async Task Should_Command_All_Asycnhronously()
        {
            var handler   = new OrderHandler();
            var fulfill   = new FulfillOrder { OrderId = 1 };
            var results   = await handler.CommandAllAsync<bool>(fulfill);
            var responses = results.ToArray();
            Assert.AreEqual(2, responses.Length);
            Assert.IsTrue(responses.All(b => b));
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Reject_Unhandled_Command()
        {
            var handler = new OrderHandler();
            handler.Command(new UpdateOrder());
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public async Task Should_Reject_Unhandled_Command_Async()
        {
            var handler = new OrderHandler();
            await handler.CommandAsync(new UpdateOrder());
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Not_Handler_Command()
        {
            new NullHandler().Command<object>(new PlaceOrder());
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public async Task Should_Not_Handler_Command_Async()
        {
            await new NullHandler().CommandAsync<object>(new CancelOrder());
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
            public Promise Process(Command command, IHandler composer)
            {
                var callback = command.Callback;
                if (callback is FulfillOrder)
                    return new Promise<bool>((resolve, reject) => resolve(true, true));
                return null;
            }
        }

        private class NullHandler : Handler
        {
            [Handles,
             Filter(typeof(IFilter<,>), Many = true)]
            public object Place(PlaceOrder order)
            {
                return null;
            }

            [Handles,
             Filter(typeof(IFilter<,>), Many = true)]
            public Promise Cancel(CancelOrder cancel)
            {
                return null;
            }


            [Provides(typeof(IFilter<,>))]
            public object CreateFilter(Inquiry inquiry)
            {
                var type = (Type)inquiry.Key;
                if (type.IsGenericTypeDefinition) return null;
                if (type.IsInterface)
                    return Activator.CreateInstance(
                        typeof(NullBehavior<,>).
                        MakeGenericType(type.GenericTypeArguments));
                return type.IsAbstract ? null
                     : Activator.CreateInstance(type);
            }
        }

        private class NullBehavior<Cb, Res> : IFilter<Cb, Res>
        {
            public int? Order { get; set; } = 1;

            public Task<Res> Next(Cb callback, MethodBinding binding,
                IHandler composer, Next<Res> next,
                IFilterProvider provider)
            {
                return next();
            }
        }
    }
}
