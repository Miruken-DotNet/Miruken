namespace Miruken.Castle.Tests
{
    using System;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Callback;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.Resolvers.SpecializedResolvers;

    /// <summary>
    /// Summary description for ResolveCallbackTests
    /// </summary>
    [TestClass]
    public class ResolveCallbackTests
    {
        protected WindsorHandler _handler;

        public interface IEmailFeature : IResolving
        {
            int Count { get; }

            int Email(string message);

            void CancelEmail(int id);
        }

        public class EmailHandler : Handler, IEmailFeature
        {
            public int Count { get; private set; }

            public int Email(string message)
            {
                if (Count > 0 && Count % 2 == 0)
                    return Protocol<IOffline>.Cast(Composer).Email(message);
                return ++Count;
            }

            public void CancelEmail(int id)
            {
                var composer = id > 4
                             ? Composer.BestEffort()
                             : Composer;
                Protocol<IBilling>.Cast(composer).Bill(4M);
            }
        }

        public interface IBilling : IResolving
        {
            decimal Bill(decimal amount);
        }

        public class Billing : IBilling
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

        public interface IOffline : IEmailFeature, IBilling
        {      
        }

        public class OfflineHandler : Handler, IOffline
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
        }

        public class DemoHandler : Handler
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

        [TestInitialize]
        public void TestInitialize()
        {
            _handler = new WindsorHandler(container =>
            {
                container.Kernel.Resolver.AddSubResolver(new ArrayResolver(container.Kernel));
                container.Install(WithFeatures.FromAssembly(
                    Assembly.GetExecutingAssembly()),
                    new HandlerInstaller());
            });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _handler.Dispose();
        }

        [TestMethod]
        public void Should_Provide_Methods()
        {
            var id = Protocol<IEmailFeature>.Cast(_handler).Email("Hello");
            Assert.AreEqual(1, id);
            id = _handler.Cast<IEmailFeature>().Email("Hello");
            Assert.AreEqual(2, id);
        }

        [TestMethod]
        public void Should_Provide_Properties()
        {
            var count = Protocol<IEmailFeature>.Cast(_handler).Count;
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void Should_Provide_Methods_Covariantly()
        {
            var id = Protocol<IEmailFeature>.Cast(_handler).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Provide_Methods_Polymorphically()
        {
            var id = Protocol<IEmailFeature>.Cast(_handler).Email("Hello");
            Assert.AreEqual(1, id);
            id = _handler.Cast<IEmailFeature>().Email("Hello");
            Assert.AreEqual(2, id);
            id = _handler.Cast<IEmailFeature>().Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Provide_Methods_Strictly()
        {
            using (var handler = new WindsorHandler(container =>
            {
                container.Register(Component.For<IOffline, IEmailFeature>()
                    .ImplementedBy<OfflineHandler>());
            }))
            {
                Protocol<IEmailFeature>.Cast(handler.Strict()).Email("22");
            }
        }

        [TestMethod]
        public void Should_Chain_Provide_Methods_Strictly()
        {
            var id = Protocol<IEmailFeature>.Cast(_handler.Strict()).Email("22");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Provide_Void_Methods()
        {
            Protocol<IEmailFeature>.Cast(_handler).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Visit_All_Providers()
        {
            using (var handler = new WindsorHandler(container =>
            {
                container.Register(
                    Component.For<IEmailFeature>()
                        .ImplementedBy<OfflineHandler>())
                    .Register(Component.For<IEmailFeature>()
                        .ImplementedBy<EmailHandler>());
            }))
            {
                Protocol<IEmailFeature>.Cast(handler).CancelEmail(13);
            }
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Ignore_Unhandled_Methods()
        {
            using (var handler = new WindsorHandler(container =>
            {
                container.Register(Component.For<IOffline>()
                    .ImplementedBy<OfflineHandler>());
            }))
            {
                Protocol<IEmailFeature>.Cast(handler).CancelEmail(13);
            }
        }
    }
}
