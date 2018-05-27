namespace Miruken.Tests.Callback
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using static Protocol;

    /// <summary>
    /// Summary description for ResolvingTests
    /// </summary>
    [TestClass]
    public class ResolvingTests
    {
        public class SendEmail
        {
            public string Body { get; set; }
        }

        public class SendEmail<T>
        {
            public T Body { get; set; }
        }

        private interface IEmailFeature : IResolving
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

        private interface IBilling : IResolving
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
                throw new NotSupportedException("Not supported offline");
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
            private readonly EmailHandler _email = new EmailHandler();

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
            private readonly DemoHandler _demo = new DemoHandler();

            [Provides]
            public DemoHandler ProvideDemo()
            {
                return _demo;
            }
        }

        private class OfflineProvider : Handler
        {
            private readonly OfflineHandler _offline = new OfflineHandler();

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

        private class AuditFilter<Cb, Res> : DynamicFilter<Cb, Res>
        {
            public Task<Res> Next(Cb callback, Next<Res> next,
                MethodBinding method,
                Repository<Message> repository,
                IBilling billing)
            {
                var send = callback as SendEmail;
                if (send != null)
                {
                    var message = new Message { Content = send.Body };
                    repository.Create(new Create<Message>(message));
                    send.Body = method.Dispatcher.Method.Name;
                    if (typeof(Res) == typeof(int))
                    {
                        billing.Bill(message.Id);
                        return Task.FromResult(
                            (Res)(object)(message.Id * 10));
                    }
                }
                return next();
            }
        }

        private class BalanceFilter<T, Res> : DynamicFilter<Create<T>, Res>
            where T : IEntity
        {
            public Task<Res> Next(Create<T> callback, Next<Res> next,
                Repository<Message> repository, IBilling billing)
            {
                return next();
            }
        }

        private class FilterProvider : Handler
        {
            [Provides]
            public AuditFilter<Cb, Res> ProviderAudit<Cb, Res>()
            {
                return new AuditFilter<Cb, Res>();
            }

            [Provides]
            public BalanceFilter<T, Res> ProviderBalance<T, Res>()
                where T: IEntity
            {
                return new BalanceFilter<T, Res>();
            }
        }

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
                return new Repository<T>();
            }
        }

        [TestMethod]
        public void Should_Override_Providers()
        {
            var demo    = new DemoHandler();
            var handler = new Handler();
            var resolve = handler.Resolve().Provide(demo).Resolve<DemoHandler>();
            Assert.AreSame(demo, resolve);
        }

        [TestMethod]
        public void Should_Override_Providers_Polymorphically()
        {
            var email   = new EmailHandler();
            var handler = new Handler();
            var resolve = handler.Resolve().Provide(email).Resolve<IEmailFeature>();
            Assert.AreSame(email, resolve);
        }

        [TestMethod]
        public void Should_Override_Providers_Resolving()
        {
            HandlerDescriptor.GetDescriptor<DemoProvider>();
            var demo    = new DemoHandler();
            var handler = new Handler();
            var resolve = handler.Provide(demo).Resolve().Resolve<DemoHandler>();
            Assert.AreSame(demo, resolve);
        }

        [TestMethod]
        public void Should_Resolve_Handlers()
        {
            HandlerDescriptor.GetDescriptor<EmailHandler>();
            var handler = new EmailProvider();
            var id      = handler.Resolve()
                .Command<int>(new SendEmail {Body = "Hello"});
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Implied_Handlers()
        {
            var handler = new EmailHandler();
            var id      = handler.Resolve()
                .Command<int>(new SendEmail { Body = "Hello" });
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Generic_Handlers()
        {
            HandlerDescriptor.GetDescriptor<EmailHandler>();
            var handler = new EmailProvider();
            var id      = handler.Resolve()
                .Command<int>(new SendEmail<int> { Body = 22 });
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_All_Handlers()
        {
            HandlerDescriptor.GetDescriptor<EmailHandler>();
            HandlerDescriptor.GetDescriptor<OfflineHandler>();
            var handler = new EmailProvider()
                        + new OfflineProvider();
            var id      = handler.ResolveAll()
                .Command<int>(new SendEmail { Body = "Hello" });
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Implied_Open_Generic_Handlers()
        {
            var handler = new Repository<Message>();
            var message = new Message();
            var handled = handler.Resolve().Handle(new Create<Message>(message));
            Assert.IsTrue(handled);
            Assert.AreEqual(1, message.Id);
        }

        [TestMethod]
        public void Should_Resolve_Open_Generic_Handlers()
        {
            HandlerDescriptor.GetDescriptor(typeof(Repository<>));
            var handler = new RepositoryProvider();
            var message = new Message();
            var handled = handler.Resolve().Handle(new Create<Message>(message));
            Assert.IsTrue(handled);
            Assert.AreEqual(1, message.Id);
        }

        [TestMethod]
        public void Should_Resolve_Handlers_With_Filters()
        {
            HandlerDescriptor.GetDescriptor<EmailHandler>();
            var handler = new EmailProvider()
                        + new Billing()
                        + new RepositoryProvider()
                        + new FilterProvider();
            var id = handler.Resolve()
                .Command<int>(new SendEmail { Body = "Hello" });
            Assert.AreEqual(10, id);
        }

        [Ignore,
        TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Should_Reject_Filters_With_Missing_Dependencies()
        {
            var handler = new Accountant()
                        + new FilterProvider();
            handler.Resolve().Command<decimal>(
                new Create<Deposit>(new Deposit { Amount = 10.0M }));
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Fail_If_No_Resolve_Handlers()
        {
            var handler = new HandlerAdapter(new Billing());
            handler.Resolve().Command<int>(new SendEmail { Body = "Hello" });
        }

        [TestMethod]
        public void Should_Provide_Methods()
        {
            var provider = new EmailProvider();
            var id       = Proxy<IEmailFeature>(provider).Email("Hello");
            Assert.AreEqual(1, id);
            id = provider.Proxy<IEmailFeature>().Email("Hello");
            Assert.AreEqual(2, id);
        }

        [TestMethod]
        public void Should_Provide_Properties()
        {
            var provider = new EmailProvider();
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
            var provider = new EmailProvider() + new OfflineProvider();
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
            var provider = new OfflineProvider() + new EmailProvider();
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
            var provider = new EmailProvider() + new BillingProvider(new Billing());
            Proxy<IEmailFeature>(provider).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Visit_All_Providers()
        {
            var provider = new ManyProvider();
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
            var provider = new OfflineHandler() + new EmailProvider();
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
        public void Should_Not_Propogate_Best_Effort()
        {
            var provider = new EmailProvider();
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
            var email  = master + mirror + backup;
            var id     = Proxy<IEmailFeature>(email.Broadcast()).Email("Hello");
            Assert.AreEqual(1, id);
            Assert.AreEqual(1, master.Resolve<EmailHandler>().Count);
            Assert.AreEqual(1, mirror.Resolve<EmailHandler>().Count);
            Assert.AreEqual(1, backup.Resolve<EmailHandler>().Count);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Inferred()
        {
            var provider = new EmailProvider();
            var id       = Proxy<IEmailFeature>(provider.Resolve()).Email("Hello");
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
