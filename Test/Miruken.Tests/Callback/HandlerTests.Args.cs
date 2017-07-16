namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Concurrency;

    [TestClass]
    public class HandlerArgsTests
    {
        [TestInitialize]
        public void Setup()
        {
            NextId = 0;
        }

        [TestMethod]
        public void Should_Resolve_Single_Dependency()
        {
            var handler = new InventoryHandler()
                        + new Repository<Order>();
            var order   = new Order
            {
                LineItems = new[]
                {
                    new LineItem { PLU = "1234", Quantity = 1 }
                }
            };
            var confirmation = handler.Command<Guid>(new NewOrder(order));
            Assert.AreNotEqual(Guid.Empty, confirmation);
            Assert.AreEqual(1, order.Id);
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Fail_If_Unresolved_Dependency()
        {
            var handler = new InventoryHandler();
            handler.Command<Guid>(new NewOrder(new Order()));
        }

        [TestMethod]
        public async Task Should_Resolve_Promie_Dependency()
        {
            var handler = new InventoryHandler()
                        + new Repository<Order>();
            var order = new Order { Id = 1 };
            await handler.CommandAsync<object>(new CancelOrder(order));
        }

        [TestMethod]
        public async Task Should_Resolve_Task_Dependency()
        {
            var handler = new InventoryHandler()
                        + new Repository<Order>();
            var order = new Order { Id = 1 };
            await handler.CommandAsync<object>(new ChangeOrder(order));
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Fail_If_Unresolved_Promise_Dependency()
        {
            var handler = new InventoryHandler();
            handler.Command<Guid>(new CancelOrder(new Order()));
        }

        [TestMethod]
        public async Task Should_Resolve_Array_Dependency()
        {
            var handler = new InventoryHandler()
                        + new CustomerSupport()
                        + new Repository<Order>();
            handler.Command(new NewOrder(new Order()));
            handler.Command(new NewOrder(new Order()));
            handler.Command(new NewOrder(new Order()));
            var order = await handler.CommandAsync<Order>(new RefundOrder { OrderId = 2 });
            Assert.IsNotNull(order);
            Assert.AreEqual(2, order.Id);
        }

        private class LineItem
        {
            public string PLU      { get; set; }
            public int    Quantity { get; set; }
        }

        private interface IEntity
        {
            int Id { get; set; }
        }

        private enum OrderStatus
        {
            Pending,
            Cancelled,
            Refunded
        }

        private class Order : IEntity
        {
            public int         Id        { get; set; }
            public OrderStatus Status    { get; set; }
            public LineItem[]  LineItems { get; set; }
        }

        private class NewOrder
        {
            public NewOrder(Order order)
            {
                Order = order;
            }
            public Order Order { get; }
        }

        private class ChangeOrder
        {
            public ChangeOrder(Order order)
            {
                Order = order;
            }
            public Order Order { get; }
        }

        private class RefundOrder
        {
            public int OrderId { get; set; }    
        }

        private class CancelOrder
        {
            public CancelOrder(Order order)
            {
                Order = order;
            }
            public Order Order { get; }
        }

        private class InventoryHandler : Handler
        {
            private readonly List<Order> _orders = new List<Order>();

            [Provides]
            public Order[] Orders => _orders.ToArray();

            [Handles]
            public Guid PlaceOrder(NewOrder place, IRepository<Order> repository)
            {
                var order = place.Order;
                order.Status = OrderStatus.Pending;
                repository.Save(order);
                _orders.Add(order);
                return Guid.NewGuid();
            }

            [Handles]
            public async Task<Guid> ChangeOrder(
                ChangeOrder change, Task<IRepository<Order>> repository)
            {
                change.Order.Status = OrderStatus.Pending;
                await (await repository).Save(change.Order);
                return Guid.NewGuid();
            }

            [Handles]
            public Promise CancelOrder(CancelOrder cancel, Promise<IRepository<Order>> repository)
            {
                cancel.Order.Status = OrderStatus.Cancelled;
                return repository.Then((r, s) =>
                {
                    r.Save(cancel.Order);
                });
            }
        }

        private class CustomerSupport : Handler
        {
            [Handles]
            public Order RefundOrder(RefundOrder refund, Order[] orders)
            {
                var order = orders.FirstOrDefault(o => o.Id == refund.OrderId);
                if (order != null) order.Status = OrderStatus.Refunded;
                return order;
            }
        }

        private interface IRepository<in T>
            where T : IEntity
        {
            Promise Save(T entity);
        }

        private class Repository<T> : IRepository<T>
            where T : IEntity
        {
            public Promise Save(T entity)
            {
                if (entity.Id <= 0)
                    entity.Id = ++NextId;
                return Promise.Empty;
            }
        }

        private static int NextId;
    }
}
