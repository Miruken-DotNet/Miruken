// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
namespace Miruken.Tests.Callback
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Callback.Policy.Bindings;
    using static Protocol;

    /// <summary>
    /// Summary description for ResolvingTests
    /// </summary>
    [TestClass]
    [SuppressMessage("ReSharper", "CA1822")]
    public class ResolvingTests
    {
        private IHandlerDescriptorFactory _factory;

        [TestInitialize]
        public void TestInitialize()
        {
            _factory = new MutableHandlerDescriptorFactory();
            _factory.RegisterDescriptor<EmailHandler>();
            _factory.RegisterDescriptor<EmailProvider>();
            _factory.RegisterDescriptor<OfflineHandler>();
            _factory.RegisterDescriptor<DemoHandler>();
            _factory.RegisterDescriptor<BillingProvider>();
            _factory.RegisterDescriptor<OfflineProvider>();
            _factory.RegisterDescriptor<DemoProvider>();
            _factory.RegisterDescriptor<ManyProvider>();
            _factory.RegisterDescriptor<FilterProvider>();
            _factory.RegisterDescriptor(typeof(Repository<>));
            _factory.RegisterDescriptor<RepositoryProvider>();
            _factory.RegisterDescriptor<Accountant>();
            _factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(_factory);
        }

        private class SendEmail
        {
            public string Body { get; set; }
        }

        private class SendEmail<T>
        {
            public T Body { get; init; }
        }

        private interface IEmailFeature : IProtocol
        {
            int Count { get; }

            int Email(string message);

            void CancelEmail(int id);
        }

        [Filter(typeof(AuditFilter<,>))]
        private class EmailHandler : Handler, IEmailFeature
        {
            public int Count { get; private set; }

            public int Email(string message)
            {
                if (Count > 0 && Count % 2 == 0)
                    return Proxy<IOffline>(Composer).Email(message);
                return ++Count;
            }

            public void CancelEmail(int id)
            {
                var composer = id > 4
                             ? Composer.BestEffort()
                             : Composer;
                Proxy<IBilling>(composer).Bill(4M);
            }

            [Handles]
            public int Send(SendEmail send)
            {
                return Email(send.Body);
            }

            [Handles]
            public int Send<T>(SendEmail<T> send)
            {
                return Email(send.Body.ToString());
            }
        }

        private interface IBilling : IProtocol
        {
            decimal Bill(decimal amount);
        }

        private class Billing : IBilling
        {
            private readonly decimal _fee;

            public Billing() : this(2M)
            {            
            }

            public Billing(decimal fee)
            {
                _fee = fee;
            }

            public decimal Bill(decimal amount)
            {
                return amount + _fee;
            }
        }

        private interface IOffline : IEmailFeature, IBilling
        {      
        }

        private class OfflineHandler : Handler, IOffline
        {
            private int _count;

            int IEmailFeature.Count => _count;

            int IEmailFeature.Email(string message)
            {
                return ++_count;
            }

            void IEmailFeature.CancelEmail(int id)
            {
                if (id == 13)
                    Unhandled();
            }

            decimal IBilling.Bill(decimal amount)
            {
                throw new NotSupportedException("Not supported offline.");
            }

            [Handles]
            public int Send(SendEmail send)
            {
                return ((IEmailFeature)this).Email(send.Body);
            }
        }

        private class DemoHandler : Handler
        {
            public int Email(string message)
            {
                return int.Parse(message);
            }

            public decimal Bill(decimal amount)
            {
                return amount * 2;
            }
        }

        private class EmailProvider : Handler
        {
            private readonly EmailHandler _email = new();

            [Provides]
            public EmailHandler ProvideEmail()
            {
                return _email;
            }
        }

        private class BillingProvider : Handler
        {
            private readonly IBilling _billing;

            public BillingProvider(IBilling billing = null)
            {
                _billing = billing ?? new Billing();
            }

            [Provides]
            public IBilling ProvideBilling()
            {
                return _billing;
            }
        }

        private class DemoProvider : Handler
        {
            private readonly DemoHandler _demo = new();

            [Provides]
            public DemoHandler ProvideDemo()
            {
                return _demo;
            }
        }

        private class OfflineProvider : Handler
        {
            private readonly OfflineHandler _offline = new();

            [Provides]
            public OfflineHandler ProvideOffline()
            {
                return _offline;
            }
        }

        private class ManyProvider : Handler
        {
            [Provides]
            public IEmailFeature[] ProvideEmail()
            {
                return new IEmailFeature[]
                {
                    new OfflineHandler(), new EmailHandler()
                };
            }
        }

        private class AuditFilter<TCb, TRes> : DynamicFilter<TCb, TRes>
        {
            public Task<TRes> Next(TCb callback, Next<TRes> next,
                MemberBinding member,
                Repository<Message> repository,
                IBilling billing)
            {
                if (callback is SendEmail send)
                {
                    var message = new Message { Content = send.Body };
                    repository.Create(new Create<Message>(message));
                    send.Body = member.Dispatcher.Member.Name;
                    if (typeof(TRes) != typeof(int)) return next();
                    billing.Bill(message.Id);
                    return Task.FromResult((TRes)(object)(message.Id * 10));
                }
                return next();
            }
        }

        private class BalanceFilter<T, TRes> : DynamicFilter<Create<T>, TRes>
            where T : IEntity
        {
            public Task<TRes> Next(Create<T> callback, Next<TRes> next,
                Repository<Message> repository, IBilling billing) => next();
        }

        private class FilterProvider : Handler
        {
            [Provides]
            public AuditFilter<TCb, TRes> ProviderAudit<TCb, TRes>() => new();

            [Provides]
            public BalanceFilter<T, TRes> ProviderBalance<T, TRes>()
                where T: IEntity => new();
        }

        public interface IDomain { }

        public class DomainContext<T> where T : IDomain
        {
            public T Domain { get; }

            public DomainContext(T domain)
            {
                Domain = domain;
            }
        }

        public class DomainRepository<T> where T : IDomain
        {
            public T Domain { get; }
            public DomainContext<T> Context { get; }

            public DomainRepository(DomainContext<T> context, T domain)
            {
                Context = context;
                Domain  = domain;
            }
        }

        public class DomainRepositoryProvider : Handler
        {
            [Provides]
            public DomainRepository<T> GetRepository<T>(
                DomainContext<T> context, T domain) where T : IDomain
            {
                return new(context, domain);
            }
        }

        public class MyDomain : IDomain {}

        public interface IEntity
        {
            int Id { get; set; }
        }

        public class Message : IEntity
        {
            public int    Id      { get; set; }
            public string Content { get; set; }
        }

        public class Deposit : IEntity
        {
            public int     Id     { get; set; }
            public decimal Amount { get; set; }
        }

        public class Withdrawal : IEntity
        {
            public int     Id     { get; set; }
            public decimal Amount { get; set; }
        }

        public class Create<T> where T : IEntity
        {
            public Create(T entity)
            {
                Entity = entity;
            }
            public T Entity { get; }    
        }

        public class Repository<T> : Handler
            where T : class, IEntity
        {
            private int _nextId = 1;

            [Handles]
            public void Create(Create<T> create)
            {
                create.Entity.Id = _nextId++;
            }
        }

        public class Accountant : Handler
        {
            private decimal _balance;

            [Handles,
             Filter(typeof(BalanceFilter<,>))]
            public decimal DepositFunds(Create<Deposit> deposit)
            {
                return _balance += deposit.Entity.Amount;
            }

            [Handles,
             Filter(typeof(BalanceFilter<,>))]
            public decimal WithdrawFunds(Create<Withdrawal> withdraw)
            {
                return _balance -= withdraw.Entity.Amount;
            }
        }

        public class RepositoryProvider : Handler
        {
            [Provides]
            public Repository<T> CreateRepository<T>()
                where T : class, IEntity
            {
                return new();
            }
        }

        [TestMethod]
        public void Should_Override_Providers()
        {
            var demo    = new DemoHandler();
            var handler = new Handler();
            var resolve = handler.Provide(demo).Resolve<DemoHandler>();
            Assert.AreSame(demo, resolve);
        }

        [TestMethod]
        public void Should_Override_Providers_Polymorphically()
        {
            var email   = new EmailHandler();
            var handler = new Handler();
            var resolve = handler.Provide(email).Resolve<IEmailFeature>();
            Assert.AreSame(email, resolve);
        }

        [TestMethod]
        public void Should_Override_Providers_Resolving()
        {
            _factory.RegisterDescriptor<DemoProvider>();
            var demo    = new DemoHandler();
            var handler = new Handler();
            var resolve = handler.Provide(demo).Resolve<DemoHandler>();
            Assert.AreSame(demo, resolve);
        }

        [TestMethod]
        public void Should_Resolve_Handlers()
        {
            _factory.RegisterDescriptor<EmailHandler>();
            var handler = new EmailProvider()
                        + new Billing()
                        + new RepositoryProvider()
                        + new FilterProvider();
            var id      = handler
                .Command<int>(new SendEmail {Body = "Hello"});
            Assert.AreEqual(10, id);
        }

        [TestMethod]
        public void Should_Resolve_Implied_Handlers()
        {
            var handler = new EmailHandler()
                        + new Billing()
                        + new RepositoryProvider()
                        + new FilterProvider();
            var id      = handler
                .Command<int>(new SendEmail { Body = "Hello" });
            Assert.AreEqual(10, id);
        }

        [TestMethod]
        public void Should_Resolve_Generic_Handlers()
        {
            _factory.RegisterDescriptor<EmailHandler>();
            var handler = new EmailProvider()
                        + new RepositoryProvider()
                        + new FilterProvider();
            var id      = handler
                .Command<int>(new SendEmail<int> { Body = 22 });
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_All_Handlers()
        {
            _factory.RegisterDescriptor<EmailHandler>();
            _factory.RegisterDescriptor<OfflineHandler>();
            var handler = new EmailProvider()
                        + new OfflineProvider();
            var id      = handler.Command<int>(new SendEmail { Body = "Hello" });
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Implied_Open_Generic_Handlers()
        {
            var handler = new Repository<Message>();
            var message = new Message();
            var handled = handler.Handle(new Create<Message>(message));
            Assert.IsTrue(handled);
            Assert.AreEqual(1, message.Id);
        }

        [TestMethod]
        public void Should_Resolve_Open_Generic_Handlers()
        {
            var handler = new RepositoryProvider();
            var message = new Message();
            var handled = handler.Handle(new Create<Message>(message));
            Assert.IsTrue(handled);
            Assert.AreEqual(1, message.Id);
        }

        [TestMethod]
        public void Should_Resolve_Open_Generic_Methods()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<MyDomain>();
            factory.RegisterDescriptor(typeof(DomainContext<>));
            factory.RegisterDescriptor<DomainRepositoryProvider>();
            HandlerDescriptorFactory.UseFactory(factory);
            var handler    = new StaticHandler();
            var repository = handler.Resolve<DomainRepository<MyDomain>>();
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        public void Should_Resolve_Handlers_With_Filters()
        {
            _factory.RegisterDescriptor<EmailHandler>();
            var handler = new EmailProvider()
                        + new Billing()
                        + new RepositoryProvider()
                        + new FilterProvider();
            var id = handler
                .Command<int>(new SendEmail { Body = "Hello" });
            Assert.AreEqual(10, id);
        }

        [Ignore, TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Should_Reject_Filters_With_Missing_Dependencies()
        {
            var handler = new Accountant()
                        + new FilterProvider();
            handler.Command<decimal>(
                new Create<Deposit>(new Deposit { Amount = 10.0M }));
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Fail_If_No_Resolve_Handlers()
        {
            var handler = new HandlerAdapter(new Billing());
            handler.Command<int>(new SendEmail { Body = "Hello" });
        }

        [TestMethod]
        public void Should_Provide_Methods()
        {
            var provider = new EmailProvider()
                         + new RepositoryProvider()
                         + new FilterProvider();
            var id       = Proxy<IEmailFeature>(provider).Email("Hello");
            Assert.AreEqual(1, id);
            id = provider.Proxy<IEmailFeature>().Email("Hello");
            Assert.AreEqual(2, id);
        }

        [TestMethod]
        public void Should_Provide_Properties()
        {
            var provider = new EmailProvider()
                         + new RepositoryProvider()
                         + new FilterProvider();
            var count    = Proxy<IEmailFeature>(provider).Count;
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void Should_Provide_Methods_Covariantly()
        {
            var provider = new OfflineProvider();
            var id       = Proxy<IEmailFeature>(provider).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Provide_Methods_Polymorphically()
        {
            var provider = new EmailProvider() 
                         + new OfflineProvider()
                         + new RepositoryProvider()
                         + new FilterProvider();
            var id = Proxy<IEmailFeature>(provider).Email("Hello");
            Assert.AreEqual(1, id);
            id = provider.Proxy<IEmailFeature>().Email("Hello");
            Assert.AreEqual(2, id);
            id = provider.Proxy<IEmailFeature>().Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Provide_Methods_Strictly()
        {
            var provider = new OfflineProvider();
            Proxy<IEmailFeature>(provider.Strict()).Email("22");
        }

        [TestMethod]
        public void Should_Chain_Provide_Methods_Strictly()
        {
            var provider = new OfflineProvider()
                         + new EmailProvider()
                         + new RepositoryProvider()
                         + new FilterProvider();
            var id = Proxy<IEmailFeature>(provider.Strict()).Email("22");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Require_Protocol_Conformance()
        {
            var provider = new DemoProvider();
            Proxy<IEmailFeature>(provider.Duck()).Email("22");
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Require_Protocol_Invariance()
        {
            var provider = new DemoProvider();
            Proxy<IOffline>(provider).Email("22");
        }

        [TestMethod]
        public void Should_Provide_Void_Methods()
        {
            var provider = new EmailProvider() 
                         + new BillingProvider(new Billing())
                         + new RepositoryProvider()
                         + new FilterProvider();
            Proxy<IEmailFeature>(provider).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Visit_All_Providers()
        {
            var provider = new ManyProvider()
                         + new RepositoryProvider()
                         + new FilterProvider();
            Proxy<IEmailFeature>(provider).CancelEmail(13);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Ignore_Unhandled_Methods()
        {
            var provider = new OfflineHandler();
            Proxy<IEmailFeature>(provider).CancelEmail(13);
        }

        [TestMethod, ExpectedException(typeof(NotSupportedException))]
        public void Should_Find_Matching_Method()
        {
            var provider = new OfflineHandler() 
                         + new EmailProvider()
                         + new RepositoryProvider()
                         + new FilterProvider();
            Proxy<IEmailFeature>(provider).CancelEmail(13);
        }

        [TestMethod]
        public void Should_Provide_Methods_Best_Effort()
        {
            var provider = new Handler();
            var id       = Proxy<IEmailFeature>(provider.BestEffort()).Email("Hello");
            Assert.AreEqual(0, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Not_Propagate_Best_Effort()
        {
            var provider = new EmailProvider()
                         + new RepositoryProvider()
                         + new FilterProvider();
            Proxy<IEmailFeature>(provider.BestEffort()).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Apply_Nested_Best_Effort()
        {
            var provider = new EmailProvider();
            Proxy<IEmailFeature>(provider.BestEffort()).CancelEmail(6);
        }

        [TestMethod]
        public void Should_Broadcast_Methods()
        {
            var master = new EmailProvider();
            var mirror = new EmailProvider();
            var backup = new EmailProvider();
            var email  = master + mirror + backup
                       + new RepositoryProvider() 
                       + new FilterProvider();
            var id     = Proxy<IEmailFeature>(email.Broadcast()).Email("Hello");
            Assert.AreEqual(1, id);
            Assert.AreEqual(1, master.Resolve<EmailHandler>().Count);
            Assert.AreEqual(1, mirror.Resolve<EmailHandler>().Count);
            Assert.AreEqual(1, backup.Resolve<EmailHandler>().Count);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Inferred()
        {
            var provider = new EmailProvider()
                         + new RepositoryProvider()
                         + new FilterProvider();
            var id       = Proxy<IEmailFeature>(provider).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Implicitly()
        {
            var provider = new BillingProvider(new Billing());
            var total    = Proxy<IBilling>(provider).Bill(7.50M);
            Assert.AreEqual(9.50M, total);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Not_Resolve_Methods_Implicitly()
        {
            var provider = new DemoProvider();
            Proxy<IBilling>(provider).Bill(15M);
        }

        [TestMethod]
        public void Should_Handle_Methods_Using_Protocol()
        {
            var billing = new BillingProvider(new Billing(4M));
            Assert.AreEqual(7M, Proxy<IBilling>(billing).Bill(3M));
        }

        [TestMethod]
        public void Should_Allow_Protocol_Cast()
        {
            var offline = Proxy<IOffline>(new BillingProvider());
            var email   = (IEmailFeature)offline;
            Assert.IsNotNull(email);
            var bill    = (IBilling)offline;
            Assert.IsNotNull(bill);
        }

        [TestMethod, ExpectedException(typeof(InvalidCastException))]
        public void Should_Reject_Invalid_Protocol_Cast()
        {
            var offline = Proxy<IOffline>(new BillingProvider());
            var handler = (IHandler) offline;
        }
    }
}
