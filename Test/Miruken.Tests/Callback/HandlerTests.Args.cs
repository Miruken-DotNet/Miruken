namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Concurrency;

    [TestClass]
    public class HandlerArgsTests
    {
        [TestInitialize]
        public void Setup()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<InventoryHandler>();
            factory.RegisterDescriptor<CustomerSupport>();
            factory.RegisterDescriptor<ConfigurationHandler>();
            factory.RegisterDescriptor<SimpleDependencyHandler>(); 
            factory.RegisterDescriptor<Stash>();
            HandlerDescriptorFactory.UseFactory(factory);

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
        public async Task Should_Resolve_Promise_Dependency()
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
         ExpectedException(typeof(InvalidOperationException))]
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

        [TestMethod]
        public async Task Should_Resolve_Task_Array_Dependency()
        {
            var handler = new InventoryHandler()
                        + new CustomerSupport()
                        + new Repository<Order>();
            handler.Command(new NewOrder(new Order()));
            handler.Command(new NewOrder(new Order()));
            handler.Command(new NewOrder(new Order()));
            var orders = await handler.CommandAsync<Order[]>(new ClockIn());
            Assert.AreEqual(3, orders.Length);
        }

        [TestMethod]
        public async Task Should_Resolve_Promise_Array_Dependency()
        {
            var handler = new InventoryHandler()
                        + new CustomerSupport()
                        + new Repository<Order>();
            handler.Command(new NewOrder(new Order()));
            handler.Command(new NewOrder(new Order()));
            var orders = await handler.CommandAsync<Order[]>(new ClockOut());
            Assert.AreEqual(2, orders.Length);
        }

        [TestMethod]
        public void Should_Resolve_Simple_Dependency()
        {
            var handler = new SimpleDependencyHandler()
                        + new ConfigurationHandler();
            var maxRetries = handler.Command<int>(new NewOrder(null));
            Assert.AreEqual(2, maxRetries);
        }

        [TestMethod]
        public void Should_Resolve_Simple_Promise_Dependency()
        {
            var handler = new SimpleDependencyHandler()
                        + new ConfigurationHandler();
            var maxRetries = handler.Command<int>(new ChangeOrder(null));
            Assert.AreEqual(2, maxRetries);
        }

        [TestMethod]
        public async Task Should_Resolve_Simple_Task_Dependency()
        {
            var handler = new SimpleDependencyHandler()
                        + new ConfigurationHandler();
            var maxRetries = await handler.CommandAsync<int>(new CancelOrder(null));
            Assert.AreEqual(2, maxRetries);
        }

        [TestMethod]
        public void Should_Resolve_Simple_Array_Dependency()
        {
            var handler = new SimpleDependencyHandler()
                        + new ConfigurationHandler();
            var help = handler.Command<string[]>(new RefundOrder());
            CollectionAssert.AreEquivalent(new []
            {
                "www.help.com",
                "www.help2.com",
                "www.help3.com"
            }, help);
        }

        [TestMethod]
        public async Task Should_Ignore_Missing_Optional_Dependency()
        {
            var handler = new CustomerSupport();
            await handler.CommandAsync(new NewOrder(null));
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

        private class ClockIn { }

        private class ClockOut { }

        private class InventoryHandler : Handler
        {
            private readonly List<Order> _orders = new List<Order>();

            [Provides]
            public Order[] Orders => _orders.ToArray();

            [Handles]
            public Guid PlaceOrder(NewOrder place, IRepository<Order> repository,
                                   Func<Order[]> getOrders)
            {
                var order = place.Order;
                order.Status = OrderStatus.Pending;
                repository.Save(order);
                _orders.Add(order);
                var orders = getOrders();
                CollectionAssert.Contains(orders, order);
                return Guid.NewGuid();
            }

            [Handles]
            public async Task<Guid> ChangeOrder(
                ChangeOrder change, Task<IRepository<Order>> repository,
                Func<Promise<Order[]>> getOrders)
            {
                change.Order.Status = OrderStatus.Pending;
                await (await repository).Save(change.Order);
                var orders = await getOrders();
                Assert.AreEqual(0, orders.Length);
                return Guid.NewGuid();
            }

            [Handles]
            public Promise CancelOrder(
                CancelOrder cancel, Promise<IRepository<Order>> repository,
                Func<Task<Order[]>> getOrders)
            {
                cancel.Order.Status = OrderStatus.Cancelled;
                var orders = getOrders();
                Assert.AreEqual(0, orders.Result.Length);
                return repository.Then((r, s) =>
                {
                    r.Save(cancel.Order);
                });
            }
        }

        private class CustomerSupport : Handler
        {
            [Handles]
            public Order RefundOrder(RefundOrder refund, Order[] orders,
                Func<IRepository<Order>> getRepository)
            {
                var order = orders.FirstOrDefault(o => o.Id == refund.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatus.Refunded;
                    getRepository().Save(order);
                }
                return order;
            }

            [Handles]
            public Promise<Order[]> ClockIn(ClockIn clockIn, Task<Order[]> orders,
                IRepository<Order> repository)
            {
                return orders;
            }

            [Handles]
            public Task<Order[]> ClockOut(ClockOut clockOut, Promise<Order[]> orders,
                IRepository<Order> repository)
            {
                return orders;
            }

            [Handles]
            public void ValidateOrder(NewOrder place, [Optional]IRepository<Order> repository)
            {
                Assert.IsNotNull(place);
                Assert.IsNull(repository);
            }
        }

        private enum LogLevel
        {
            Trace = 0,
            Debug = 1,
            Info  = 2,
            Warn  = 3,
            Error = 4,
            Fatal = 5,
            Off
        }

        private class ConfigurationHandler : Handler
        {
            [Provides]
            public int MaxRetries => 2;

            [Provides("logLevel", StringComparison.OrdinalIgnoreCase)]
            public int LogLevelInt => (int)LogLevel.Info;

            [Provides("logLevelStr", StringComparison.OrdinalIgnoreCase)]
            public string LogLevelStr => LogLevel.Fatal.ToString();

            [Provides("help")]
            public string PrimaryHelp => "www.help.com";

            [Provides("help")]
            public string SecondaryHelp => "www.help2.com";

            [Provides("help")]
            public string CriticalHelp => "www.help3.com";

        }

        private class SimpleDependencyHandler : Handler
        {
            [Handles]
            public int Place(NewOrder newOrder, int maxRetries, LogLevel logLevel)
            {
                Assert.AreEqual(LogLevel.Info, logLevel);
                return maxRetries;
            }

            [Handles]
            public async Task<int> Change(ChangeOrder changeOrder,
                Promise<int> maxRetries, Promise<LogLevel> logLevel)
            {
                Assert.AreEqual(LogLevel.Info, await logLevel);
                return await maxRetries;
            }

            [Handles]
            public async Task<int> Cancel(CancelOrder cancelOrder,
                Task<int> maxRetries, [Key("logLevelStr")]Task<LogLevel> logLevel)
            {
                Assert.AreEqual(LogLevel.Fatal, await logLevel);
                return await maxRetries;
            }

            [Handles]
            public string[] Refund(RefundOrder refundOrder, string[] help)
            {
                return help;
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
