namespace Miruken.Castle.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Callback;
    using Context;
    using Error;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using static Protocol;

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

            public event ContextChangingDelegate ContextChanging;
            public event ContextChangedDelegate ContextChanged;

            public ICar[]   Cars    { get; set; }
            public Context Context { get; set; }
            public object[] Junk => _junkyard.Parts;

            public bool Dispose(object part)
            {
                var index = Array.IndexOf(Cars, part);
                if (index < 0)
                    throw new ArgumentException("Part not found");
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
            var car = _handler.BestEffort().Resolve<ICar>();
            Assert.IsNull(car);
        }

        [TestMethod]
        public void Should_Resolve_Type_Implicitly()
        {
            _container.Register(Component.For<ICar>().ImplementedBy<Car>());
            Assert.IsNotNull(_handler.Resolve<ICar>());
        }

        [TestMethod]
        public void Should_Resolve_Type_Explicitly()
        {
            _container.Register(Component.For<ICar>().ImplementedBy<Car>());
            var car = _handler.Resolve<ICar>();
            Assert.IsNotNull(car);
        }

        [TestMethod]
        public void Should_Resolve_All_Types()
        {
            _container.Register(Component.For<ICar>().ImplementedBy<Car>());
            var cars = _handler.ResolveAll<ICar>();
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
                .Install(new FeaturesInstaller(new HandleFeature())
                    .Use(Classes.FromThisAssembly()));

            var auctions = context.ResolveAll<IAuction>();
            Assert.AreEqual(2, auctions.Length);
            CollectionAssert.Contains(auctions, auction);
        }

        [TestMethod]
        public void Should_Resolve_IResolver_Implementations()
        {
            _container.Register(
                Component.For<ICar>().ImplementedBy<Car>())
                .Install(new FeaturesInstaller(new HandleFeature())
                    .Use(Classes.FromThisAssembly()));

            var cars = Proxy<IAuction>(_handler).Cars;
            Assert.AreEqual(1, cars.Length);

            Assert.IsTrue(Proxy<IAuction>(_handler).Dispose(cars[0]));
            cars = Proxy<IAuction>(_handler).Cars;
            Assert.AreEqual(0, cars.Length);
        }

        [TestMethod]
        public void Should_Register_All_IResolving_Services()
        {
            _container.Install(
                new FeaturesInstaller(new HandleFeature())
                    .Use(Classes.FromThisAssembly()));
            var auction = _handler.Resolve<IAuction>();
            var closing = _handler.Resolve<IClosing>();
            Assert.AreSame(auction, closing);
        }

        [TestMethod]
        public void Should_Skip_IResolving_Service()
        {
            _container.Install(
                new FeaturesInstaller(new HandleFeature())
                    .Use(Classes.FromThisAssembly()));
            var resolving = _handler.BestEffort().ResolveAll<IResolving>();
            Assert.AreEqual(0, resolving.Length);
        }

        [TestMethod]
        public void Should_Resolve_Dependencies_Externally()
        {
            var context  = new Context();
            context.AddHandlers(_handler);

            _container.Register(
                Component.For<ICar>().ImplementedBy<Car>())
                .Install(new FeaturesInstaller(new HandleFeature())
                    .WithExternalDependencies()
                    .Use(Classes.FromThisAssembly()));

            var auction = context.Provide(new Junkyard())
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
                .Install(new FeaturesInstaller(new HandleFeature())
                    .Use(Classes.FromThisAssembly()));

            Proxy<IAuction>(context.Publish()).Dispose(ferrari);

            var auction = context.Resolve<IAuction>();
            CollectionAssert.AreEqual(new [] { ferrari }, auction.Junk);
        }

        [TestMethod]
        public void Should_Handle_Errors()
        {
            var context = new Context();
            context.AddHandlers(_handler);
            context.Store(new Junkyard());

            var ferrari = new Car { Make = "Ferrari", Model = "LaFerrari" };

            _container
                .Install(new FeaturesInstaller(new HandleFeature())
                    .Use(Classes.FromAssemblyContaining<ErrorsHandler>(),
                        Classes.FromThisAssembly()));

            Proxy<IAuction>(context.Recover()).Dispose(ferrari);
        }
    }
}
