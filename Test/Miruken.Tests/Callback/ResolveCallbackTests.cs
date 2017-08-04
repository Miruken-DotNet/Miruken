namespace Miruken.Tests.Callback
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using static Protocol;

    /// <summary>
    /// Summary description for ResolveCallbackTests
    /// </summary>
    [TestClass]
    public class ResolveCallbackTests
    {
        public class SendEmail
        {
            public string Message { get; set; }
        }

        public class SendEmail<T>
        {
            public T Message { get; set; }
        }

        private interface IEmailFeature : IResolving
        {
            int Count { get; }

            int Email(string message);

            void CancelEmail(int id);
        }

        private class EmailHandler : Handler, IEmailFeature
        {
            public int Count { get; private set; }

            public int Email(string message)
            {
                if (Count > 0 && Count % 2 == 0)
                    return id<IOffline>(Composer).Email(message);
                return ++Count;
            }

            public void CancelEmail(int id)
            {
                var composer = id > 4
                             ? Composer.BestEffort()
                             : Composer;
                id<IBilling>(composer).Bill(4M);
            }

            [Handles]
            public int Send(SendEmail send)
            {
                return Email(send.Message);
            }

            [Handles]
            public int Send<T>(SendEmail<T> send)
            {
                return Email(send.Message.ToString());
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
                return ((IEmailFeature)this).Email(send.Message);
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

        private interface IEntity
        {
            int Id { get; set; }
        }

        public class Message : IEntity
        {
            public int    Id      { get; set; }
            public string Content { get; set; }
        }

        private class Create<T> where T : IEntity
        {
            public Create(T entity)
            {
                Entity = entity;
            }
            public T Entity { get; }    
        }

        private class Repository<T> : Handler
            where T : IEntity
        {
            private int _nextId = 1;

            [Handles]
            public void Create(Create<T> create)
            {
                create.Entity.Id = _nextId++;
            }
        }

        private class RepositoryProvider : Handler
        {
            [Provides]
            public Repository<T> CreateRepository<T>()
                where T : IEntity
            {
                return new Repository<T>();
            }
        }

        [TestMethod]
        public void Should_Resolve_Handlers()
        {
            HandlerDescriptor.GetDescriptor<EmailHandler>();
            var handler = new EmailProvider();
            var id      = handler.Resolve()
                .Command<int>(new SendEmail {Message = "Hello"});
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Implied_Handlers()
        {
            HandlerDescriptor.GetDescriptor<EmailHandler>();
            var handler = new EmailHandler();
            var id      = handler.Resolve()
                .Command<int>(new SendEmail { Message = "Hello" });
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Generic_Handlers()
        {
            HandlerDescriptor.GetDescriptor<EmailHandler>();
            var handler = new EmailProvider();
            var id      = handler.Resolve()
                .Command<int>(new SendEmail<int> { Message = 22 });
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
                .Command<int>(new SendEmail { Message = "Hello" });
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Implied_Open_Generic_Handlers()
        {
            HandlerDescriptor.GetDescriptor(typeof(Repository<>));
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

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public void Should_Fail_If_No_Resolve_Handlers()
        {
            var handler = new HandlerAdapter(new Billing());
            handler.Resolve().Command<int>(new SendEmail { Message = "Hello" });
        }

        [TestMethod]
        public void Should_Provide_Methods()
        {
            var provider = new EmailProvider();
            var id       = id<IEmailFeature>(provider).Email("Hello");
            Assert.AreEqual(1, id);
            id = provider.id<IEmailFeature>().Email("Hello");
            Assert.AreEqual(2, id);
        }

        [TestMethod]
        public void Should_Provide_Properties()
        {
            var provider = new EmailProvider();
            var count    = id<IEmailFeature>(provider).Count;
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void Should_Provide_Methods_Covariantly()
        {
            var provider = new OfflineProvider();
            var id       = id<IEmailFeature>(provider).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Provide_Methods_Polymorphically()
        {
            var provider = new EmailProvider() + new OfflineProvider();
            var id = id<IEmailFeature>(provider).Email("Hello");
            Assert.AreEqual(1, id);
            id = provider.id<IEmailFeature>().Email("Hello");
            Assert.AreEqual(2, id);
            id = provider.id<IEmailFeature>().Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Provide_Methods_Strictly()
        {
            var provider = new OfflineProvider();
            id<IEmailFeature>(provider.Strict()).Email("22");
        }

        [TestMethod]
        public void Should_Chain_Provide_Methods_Strictly()
        {
            var provider = new OfflineProvider() + new EmailProvider();
            var id = id<IEmailFeature>(provider.Strict()).Email("22");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Require_Protocol_Conformance()
        {
            var provider = new DemoProvider();
            id<IEmailFeature>(provider.Duck()).Email("22");
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Require_Protocol_Invariance()
        {
            var provider = new DemoProvider();
            id<IOffline>(provider).Email("22");
        }

        [TestMethod]
        public void Should_Provide_Void_Methods()
        {
            var provider = new EmailProvider() + new BillingProvider(new Billing());
            id<IEmailFeature>(provider).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Visit_All_Providers()
        {
            var provider = new ManyProvider();
            id<IEmailFeature>(provider).CancelEmail(13);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Ignore_Unhandled_Methods()
        {
            var provider = new OfflineHandler();
            id<IEmailFeature>(provider).CancelEmail(13);
        }

        [TestMethod, ExpectedException(typeof(NotSupportedException))]
        public void Should_Find_Matching_Method()
        {
            var provider = new OfflineHandler() + new EmailProvider();
            id<IEmailFeature>(provider).CancelEmail(13);
        }

        [TestMethod]
        public void Should_Provide_Methods_Best_Effort()
        {
            var provider = new EmailProvider();
            var id       = id<IEmailFeature>(provider.BestEffort()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Not_Propogate_Best_Effort()
        {
            var provider = new EmailProvider();
            id<IEmailFeature>(provider.BestEffort()).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Apply_Nested_Best_Effort()
        {
            var provider = new EmailProvider();
            id<IEmailFeature>(provider.BestEffort()).CancelEmail(6);
        }

        [TestMethod]
        public void Should_Broadcast_Methods()
        {
            var master = new EmailProvider();
            var mirror = new EmailProvider();
            var backup = new EmailProvider();
            var email  = master + mirror + backup;
            var id     = id<IEmailFeature>(email.Broadcast()).Email("Hello");
            Assert.AreEqual(1, id);
            Assert.AreEqual(1, master.Resolve<EmailHandler>().Count);
            Assert.AreEqual(1, mirror.Resolve<EmailHandler>().Count);
            Assert.AreEqual(1, backup.Resolve<EmailHandler>().Count);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Inferred()
        {
            var provider = new EmailProvider();
            var id       = id<IEmailFeature>(provider.Resolve()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Explicitly()
        {
            var provider = new EmailProvider();
            var id       = id<IEmailFeature>(provider.Resolve()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Implicitly()
        {
            var provider = new BillingProvider(new Billing());
            var total    = id<IBilling>(provider).Bill(7.50M);
            Assert.AreEqual(9.50M, total);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Not_Resolve_Methods_Implicitly()
        {
            var provider = new DemoProvider();
            id<IBilling>(provider).Bill(15M);
        }

        [TestMethod]
        public void Should_Handle_Methods_Using_Protocol()
        {
            var billing = new BillingProvider(new Billing(4M));
            Assert.AreEqual(7M, id<IBilling>(billing).Bill(3M));
        }

        [TestMethod]
        public void Should_Allow_Protocol_Cast()
        {
            var offline = id<IOffline>(new BillingProvider());
            var email   = (IEmailFeature)offline;
            Assert.IsNotNull(email);
            var bill    = (IBilling)offline;
            Assert.IsNotNull(bill);
        }

        [TestMethod, ExpectedException(typeof(InvalidCastException))]
        public void Should_Reject_Invalid_Protocol_Cast()
        {
            var offline = id<IOffline>(new BillingProvider());
            var handler = (IHandler) offline;
        }
    }
}
