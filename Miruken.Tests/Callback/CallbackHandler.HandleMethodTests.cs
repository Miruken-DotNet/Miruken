using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Callback;

namespace Miruken.Tests.Callback
{
    /// <summary>
    /// Summary description for CallbackHandler
    /// </summary>
    [TestClass]
    public class CallbackHandlerHandleMethodTests
    {
        interface IEmailFeature
        {
            int Email(string message);

            void CancelEmail(int id);
        }

        class EmailHandler : CallbackHandler, IEmailFeature
        {
            public int Count { get; private set; }

            public int Email(string message)
            {
                return ++Count;
            }

            public void CancelEmail(int id)
            {
                var composer = id > 4
                             ? Composer.BestEffort()
                             : Composer;
                new IBilling(composer).Bill(4M);
            }
        }

        #region Protocol
        [ComImport,
         Guid(Protocol.Guid),
         CoClass(typeof(BillingProtocol))]
        #endregion
        interface IBilling : IResolving
        {
            Decimal Bill(Decimal amount);
        }

        #region BillingProtocol

        class BillingProtocol : Protocol, IBilling
        {
            public BillingProtocol(IProtocolAdapter adapter)
                : base(adapter)
            {       
            }

            public decimal Bill(decimal amount)
            {
                return Do((IBilling p) => p.Bill(amount));
            }
        }

        #endregion

        class Billing : IBilling
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

        [TestMethod]
        public void Should_Handle_Methods()
        {
            var handler = new EmailHandler();
            var id      = handler.Do((IEmailFeature f) => f.Email("Hello"));
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Handle_Void_Methods()
        {
            var handler = new EmailHandler().Chain(new CallbackHandler(new Billing()));
            handler.Do((IEmailFeature f) => f.CancelEmail(1));
        }

        [TestMethod]
        public void Should_Handle_Methods_Best_Effort()
        {
            var handler = new EmailHandler();
            var id = handler.BestEffort().Do((IEmailFeature f) => f.Email("Hello"));
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Not_Propogate_Best_Effort()
        {
            var handler = new EmailHandler();
            handler.BestEffort().Do((IEmailFeature f) => f.CancelEmail(1));
        }

        [TestMethod]
        public void Should_Apply_Nested_Best_Effort()
        {
            var handler = new EmailHandler();
            handler.BestEffort().Do((IEmailFeature f) => f.CancelEmail(6));
        }

        [TestMethod]
        public void Should_Broadcast_Methods()
        {
            var master = new EmailHandler();
            var mirror = new EmailHandler();
            var backup = new EmailHandler();
            var email  = master.Chain(mirror, backup);
            var id     = email.Broadcast().Do((IEmailFeature f) => f.Email("Hello"));
            Assert.AreEqual(1, id);
            Assert.AreEqual(1, master.Count);
            Assert.AreEqual(1, mirror.Count);
            Assert.AreEqual(1, backup.Count);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Reject_Unhandled_Methods()
        {
            var handler = new CallbackHandler();
            handler.Do((IEmailFeature f) => f.Email("Hello"));
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Reject_Unhandled_Method_Broadcast()
        {
            var handler = new CallbackHandler().Chain(new CallbackHandler());
            handler.Do((IEmailFeature f) => f.Email("Hello"));
        }

        [TestMethod]
        public void Should_Ignore_Unhandled_Methods_If_Best_Effort()
        {
            var handler = new CallbackHandler();
            handler.BestEffort().Do((IEmailFeature f) => f.Email("Hello"));
        }

        [TestMethod]
        public void Should_Resolve_Methods_Inferred()
        {
            var handler = new EmailHandler();
            var id      = handler.Resolve().Do((IEmailFeature f) => f.Email("Hello"));
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Explicitly()
        {
            var handler = new EmailHandler();
            var id      = handler.Resolve().Do((IEmailFeature f) => f.Email("Hello"));
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Implicitly()
        {
            var handler = new CallbackHandler(new Billing());
            var total   = handler.Do((IBilling f) => f.Bill(7.50M));
            Assert.AreEqual(9.50M, total);
        }

        [TestMethod]
        public void Should_Handle_Methods_Using_Protocol()
        {
            var billing = new CallbackHandler(new Billing(4M));
            Assert.AreEqual(7M, new IBilling(billing).Bill(3M));
        }
    }
}
