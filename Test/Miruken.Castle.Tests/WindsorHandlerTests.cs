﻿using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Callback;
using Miruken.Container;
using static Miruken.Protocol;

namespace Miruken.Castle.Tests
{
    using System.Reflection;
    using Context;

    [TestClass]
    public class WindsorHandlerTests
    {
        protected IWindsorContainer _container;
        protected WindsorHandler _handler;

        public interface ICar
        {
            string Make  { get; set; }
            string Model { get; set; }
        }

        public class Car : ICar
        {
            public string Make  { get; set; }
            public string Model { get; set; }
        }

        public interface IClosing : IResolving
        {
            void Close();
        }

        public interface IJunkyard : IResolving
        {
            object[] Parts  { get; }
            void     Decommision(object part);
        }

        public interface IAuction : IResolving, IContextual
        {
            ICar[]   Cars { get; }
            object[] Junk { get; }
            bool Dispose(object part);
        }

        public class Junkyard : IJunkyard
        {
            private readonly List<object> _parts = new List<object>();

            public object[] Parts => _parts.ToArray();

            public void Decommision(object part)
            {
                _parts.Add(part);
            }
        }

        public class Auction : Handler, IAuction, IClosing
        {
            private readonly IJunkyard _junkyard;

            public Auction(IJunkyard junkyard)
            {
                _junkyard = junkyard;
                Cars      = Array.Empty<ICar>();
            }

            public event ContextChangingDelegate<IContext> ContextChanging;
            public event ContextChangedDelegate<IContext> ContextChanged;

            public ICar[]   Cars    { get; set; }
            public IContext Context { get; set; }
            public object[] Junk => _junkyard.Parts;

            public bool Dispose(object part)
            {
                var index = Array.IndexOf(Cars, part);
                if (index < 0) return false;
                _junkyard.Decommision(part);
                Cars = Cars.Where((c, i) => i != index).ToArray();
                return true;
            }

            public void Close()
            {
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _container = new WindsorContainer();
            _container.Kernel.Resolver.AddSubResolver(new ArrayResolver(_container.Kernel));
            _handler = new WindsorHandler(_container);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
        }

        [TestMethod]
        public void Should_Resolve_Nothing()
        {
            var car = P<IContainer>(_handler.BestEffort()).Resolve<ICar>();
            Assert.IsNull(car);
        }

        [TestMethod]
        public void Should_Resolve_Type_Implicitly()
        {
            _container.Register(Component.For<ICar>().ImplementedBy<Car>());
            Assert.IsNotNull(_handler.Resolve<ICar>());
        }

        [TestMethod]
        public void Should_Resolve_Type_Explicity()
        {
            _container.Register(Component.For<ICar>().ImplementedBy<Car>());
            var car = P<IContainer>(_handler).Resolve<ICar>();
            Assert.IsNotNull(car);
        }

        [TestMethod]
        public void Should_Resolve_All_Types()
        {
            _container.Register(Component.For<ICar>().ImplementedBy<Car>());
            var cars = P<IContainer>(_handler).ResolveAll<ICar>();
            Assert.AreEqual(1, cars.Length);
            Assert.IsInstanceOfType(cars, typeof(ICar[]));
        }

        [TestMethod]
        public void Should_Resolve_All_Types_With_External()
        {
            var context = new Context();
            var auction = new Auction(new Junkyard());
            context.AddHandlers(_handler, auction);

            _container.Register(
                Component.For<ICar>().ImplementedBy<Car>())
                .Install(Plugin.FromAssembly(
                            Assembly.GetExecutingAssembly()),
                         new ResolvingInstaller());

            var auctions = context.ResolveAll<IAuction>();
            Assert.AreEqual(2, auctions.Length);
            CollectionAssert.Contains(auctions, auction);
        }

        [TestMethod]
        public void Should_Resolve_IResolver_Implementations()
        {
            _container.Register(
                Component.For<ICar>().ImplementedBy<Car>())
                .Install(Plugin.FromAssembly(
                            Assembly.GetExecutingAssembly()),
                         new ResolvingInstaller());

            var cars = P<IAuction>(_handler).Cars;
            Assert.AreEqual(1, cars.Length);

            Assert.IsTrue(P<IAuction>(_handler).Dispose(cars[0]));
            cars = P<IAuction>(_handler).Cars;
            Assert.AreEqual(0, cars.Length);
        }

        [TestMethod]
        public void Should_Register_All_IResolving_Services()
        {
            _container.Install(Plugin.FromAssembly(
                                   Assembly.GetExecutingAssembly()),
                               new ResolvingInstaller());
            var auction = P<IContainer>(_handler).Resolve<IAuction>();
            var closing = P<IContainer>(_handler).Resolve<IClosing>();
            Assert.AreSame(auction, closing);
        }

        [TestMethod]
        public void Should_Skip_IResolving_Service()
        {
            _container.Install(Plugin.FromAssembly(
                                   Assembly.GetExecutingAssembly()),
                               new ResolvingInstaller());
            var resolving = P<IContainer>(_handler.BestEffort()).ResolveAll<IResolving>();
            Assert.AreEqual(0, resolving.Length);
        }

        [TestMethod]
        public void Should_Resolve_Dependencies_Externally()
        {
            var context  = new Context();
            context.AddHandlers(_handler);

            _container.Register(
                Component.For<ICar>().ImplementedBy<Car>())
                .Install(Plugin.FromAssembly(Assembly.GetExecutingAssembly()),
                         new ResolvingInstaller());

            var auction = P<IContainer>(context.Provide(new Junkyard()))
                .Resolve<IAuction>();
            Assert.AreSame(context, auction.Context);
            Assert.AreEqual(1, auction.Cars.Length);

            auction.Dispose(auction.Cars[0]);
            Assert.AreEqual(0, auction.Cars.Length);
        }

        [TestMethod]
        public void Should_Resolve_When_Publishing()
        {
            var context = new Context();
            context.AddHandlers(_handler);
            context.Store(new Junkyard());

            var ferrari = new Car { Make = "Ferrari", Model = "LaFerrari" };

            _container.Register(
                Component.For<ICar>().Instance(ferrari))
                .Install(Plugin.FromAssembly(Assembly.GetExecutingAssembly()),
                         new ResolvingInstaller());

            P<IAuction>(context.Publish()).Dispose(ferrari);

            var auction = P<IContainer>(context).Resolve<IAuction>();
            CollectionAssert.AreEqual(new [] { ferrari }, auction.Junk);
        }
    }
}
