namespace Miruken.Tests.Callback
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using static Protocol;

    /// <summary>
    /// Summary description for HandleMethodTests
    /// </summary>
    [TestClass]
    public class HandleMethodTests
    {
        private interface IEmailFeature
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
                    return P<IOffline>(Composer).Email(message);
                return ++Count;
            }

            public void CancelEmail(int id)
            {
                var composer = id > 4
                             ? Composer.BestEffort()
                             : Composer;
                P<IBilling>(composer).Bill(4M);
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
            }

            decimal IBilling.Bill(decimal amount)
            {
                throw new NotSupportedException("Not supported offline");
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

        [TestMethod]
        public void Should_Handle_Methods()
        {
            var handler = new EmailHandler();
            var id      = P<IEmailFeature>(handler).Email("Hello");
            Assert.AreEqual(1, id);
            id = handler.P<IEmailFeature>().Email("Hello");
            Assert.AreEqual(2, id);
        }

        [TestMethod]
        public void Should_Handle_Properties()
        {
            var handler = new EmailHandler();
            var count   = P<IEmailFeature>(handler).Count;
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void Should_Handle_Methods_Covariantly()
        {
            var handler = new OfflineHandler();
            var id = P<IEmailFeature>(handler).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Handle_Methods_Polymorphically()
        {
            var handler = new EmailHandler() + new OfflineHandler();
            var id = P<IEmailFeature>(handler).Email("Hello");
            Assert.AreEqual(1, id);
            id = P<IEmailFeature>(handler).Email("Hello");
            Assert.AreEqual(2, id);
            id = P<IEmailFeature>(handler).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Handle_Methods_Strictly()
        {
            var handler = new OfflineHandler();
            P<IEmailFeature>(handler.Strict()).Email("22");
        }

        [TestMethod]
        public void Should_Chain_Handle_Methods_Strictly()
        {
            var handler = new OfflineHandler() + new EmailHandler();
            var id = P<IEmailFeature>(handler.Strict()).Email("22");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Handle_Methods_Loosely()
        {
            var handler = new DemoHandler();
            var id = P<IEmailFeature>(handler.Duck()).Email("22");
            Assert.AreEqual(22, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Require_Protocol_Conformance()
        {
            var handler = new DemoHandler();
            P<IEmailFeature>(handler).Email("22");
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Require_Protocol_Invariance()
        {
            var handler = new DemoHandler();
            P<IOffline>(handler).Email("22");
        }

        [TestMethod]
        public void Should_Handle_Void_Methods()
        {
            var handler = new EmailHandler() + new Handler(new Billing());
            P<IEmailFeature>(handler).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Handle_Methods_Best_Effort()
        {
            var handler = new EmailHandler();
            var id      = P<IEmailFeature>(handler.BestEffort()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Not_Propogate_Best_Effort()
        {
            var handler = new EmailHandler();
            P<IEmailFeature>(handler.BestEffort()).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Apply_Nested_Best_Effort()
        {
            var handler = new EmailHandler();
            P<IEmailFeature>(handler.BestEffort()).CancelEmail(6);
        }

        [TestMethod]
        public void Should_Broadcast_Methods()
        {
            var master = new EmailHandler();
            var mirror = new EmailHandler();
            var backup = new EmailHandler();
            var email  = master.Chain(mirror, backup);
            var id     = P<IEmailFeature>(email.Broadcast()).Email("Hello");
            Assert.AreEqual(1, id);
            Assert.AreEqual(1, master.Count);
            Assert.AreEqual(1, mirror.Count);
            Assert.AreEqual(1, backup.Count);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Reject_Unhandled_Methods()
        {
            var handler = new Handler();
            P<IEmailFeature>(handler).Email("Hello");
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Reject_Unhandled_Method_Broadcast()
        {
            var handler = new Handler().Chain(new Handler());
            P<IEmailFeature>(handler).Email("Hello");
        }

        [TestMethod]
        public void Should_Ignore_Unhandled_Methods_If_Best_Effort()
        {
            var handler = new Handler();
            P<IEmailFeature>(handler.BestEffort()).Email("Hello");
        }

        [TestMethod]
        public void Should_Resolve_Methods_Inferred()
        {
            var handler = new EmailHandler();
            var id      = P<IEmailFeature>(handler.Resolve()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Explicitly()
        {
            var handler = new EmailHandler();
            var id      = P<IEmailFeature>(handler.Resolve()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Implicitly()
        {
            var handler = new Handler(new Billing());
            var total   = P<IBilling>(handler).Bill(7.50M);
            Assert.AreEqual(9.50M, total);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Not_Resolve_Methods_Implicitly()
        {
            var handler = new DemoHandler();
            P<IBilling>(handler).Bill(15M);
        }

        [TestMethod]
        public void Should_Handle_Methods_Using_Protocol()
        {
            var billing = new Handler(new Billing(4M));
            Assert.AreEqual(7M, P<IBilling>(billing).Bill(3M));
        }

        [TestMethod]
        public void Should_Allow_Protocol_Cast()
        {
            var offline = P<IOffline>(new Handler());
            var email   = (IEmailFeature)offline;
            Assert.IsNotNull(email);
            var bill    = (IBilling)offline;
            Assert.IsNotNull(bill);
        }

        [TestMethod]
        public void Should_Allow_Duck_Typing()
        {
            var offline = P(new Handler());
            var email = (IEmailFeature)offline;
            Assert.IsNotNull(email);
            var bill = (IBilling)offline;
            Assert.IsNotNull(bill);
        }

        [TestMethod, ExpectedException(typeof(InvalidCastException))]
        public void Should_Reject_Invalid_Protocol_Cast()
        {
            var offline = P<IOffline>(new Handler());
            var handler = (IHandler) offline;
        }

        [TestMethod, ExpectedException(typeof(NotSupportedException),
            "Only protocol interfaces are supported")]
        public void Should_Reject_Non_Interface_Cast()
        {
            P<OfflineHandler>(new Handler());
        }
    }
}
