namespace Miruken.Castle.Tests
{
    using Callback;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HandlerFeatureTests
    {
        protected IWindsorContainer _container;

        [TestInitialize]
        public void TestInitialize()
        {
            _container = new WindsorContainer();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
        }

        [TestMethod]
        public void Should_Register_Handlers()
        {
            _container.Install(
                new FeaturesInstaller(new HandlerFeature())
                    .Use(Classes.FromThisAssembly()));
            var handler = _container.Resolve<MyHandler>();
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Should_Resolve_Handler()
        {
            using (var handler = new WindsorHandler(container =>
            {
                container.Install(
                    new FeaturesInstaller(new HandlerFeature())
                        .Use(Classes.FromThisAssembly()));
            }))
            {
                Assert.IsTrue(handler.Resolve().Handle(new A()));
            } 
        }

        public class A { }
        public class B { }

        public class MyHandler : Handler
        {
            [Handles]
            public void HandlesA(A a)
            {                
            }

            [Provides]
            public B ProvidesB()
            {
                return new B();
            }
        }

        public abstract class MyAbstractHansler : Handler
        {
            [Handles]
            public void HandlesA(A a)
            {
            }
        }
    }
}
