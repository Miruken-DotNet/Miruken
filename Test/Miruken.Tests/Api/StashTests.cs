namespace Miruken.Tests.Api
{
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Callback.Policy.Bindings;

    [TestClass]
    public class StashTests
    {
        public enum OrderStatus
        {
            Created,
            Cancelled
        }

        public class Order
        {
            public int         Id     { get; set; }
            public OrderStatus Status { get; set; }
        }

        public class CancelOrder
        {
            public CancelOrder(int orderId)
            {
                OrderId = orderId;
            }
            public int OrderId { get; }
        }

        public class OrderHandler : Handler
        {
            [Handles, Filter(typeof(CancelOrderFilter))]
            public Order Cancel(CancelOrder cancel,
                IHandler composer, Order order)
            {
                order.Status = OrderStatus.Cancelled;
                return order;
            }
        }

        public class CancelOrderFilter : IFilter<CancelOrder, Order>
        {
            public int? Order { get; set; } = Stage.Filter;

            public Task<Order> Next(CancelOrder cancelOrder,
                object rawCallback, MemberBinding binding,
                IHandler composer, Next<Order> next,
                IFilterProvider provider)
            {
                var order = new Order { Id = cancelOrder.OrderId };
                composer.Proxy<IStash>().Put(order);
                return next();
            }
        }

        private IHandlerDescriptorFactory _factory;

        [TestInitialize]
        public void TestInitialize()
        {
            _factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(_factory);
        }

        [TestMethod]
        public void Should_Add_To_Stash()
        {
            var order   = new Order();
            var handler = new Stash();
            handler.Proxy<IStash>().Put(order);
            Assert.AreSame(order, handler.Proxy<IStash>().Get<Order>());
        }

        [TestMethod]
        public void Should_Get_Or_Add_To_Stash()
        {
            var order   = new Order();
            var handler = new Stash();
            var result  = handler.Proxy<IStash>().GetOrPut(order);
            Assert.AreSame(order, result);
            Assert.AreSame(order, handler.Proxy<IStash>().Get<Order>());
        }

        [TestMethod]
        public void Should_Drop_From_Stash()
        {
            var order   = new Order();
            var stash   = new Stash();
            var handler = stash + new Stash(true);
            handler.Proxy<IStash>().Put(order);
            handler.Proxy<IStash>().Drop<Order>();
            Assert.IsNull(handler.Proxy<IStash>().Get<Order>());
        }

        [TestMethod]
        public void Should_Cascade_Stash()
        {
            var order    = new Order();
            var handler  = new Stash();
            var handler2 = new Stash() + handler;
            handler.Proxy<IStash>().Put(order);
            Assert.AreSame(order, handler2.Proxy<IStash>().Get<Order>());
        }

        [TestMethod]
        public void Should_Hide_Stash()
        {
            var order    = new Order();
            var handler  = new Stash();
            var handler2 = new Stash() + handler;
            handler.Proxy<IStash>().Put(order);
            handler2.Proxy<IStash>().Put<Order>(null);
            Assert.IsNull(handler2.Proxy<IStash>().Get<Order>());
        }

        [TestMethod]
        public async Task Should_Access_Stash()
        {
            var handler = new OrderHandler() + new CancelOrderFilter();

            var order = await handler.Send<Order>(new CancelOrder(1));
            Assert.IsNotNull(order);
            Assert.AreEqual(1, order.Id);
            Assert.AreEqual(OrderStatus.Cancelled, order.Status);
        }
    }
}
