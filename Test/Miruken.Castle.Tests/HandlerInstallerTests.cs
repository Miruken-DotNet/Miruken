namespace Miruken.Castle.Tests
{
    using System.Reflection;
    using Callback;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HandlerInstallerTests
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
            var assembly = Assembly.GetExecutingAssembly();
            _container.Install(new HandlerInstaller(),
                WithFeatures.FromAssembly(assembly));
            var handler = _container.Resolve<MyHandler>();
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Should_Resolve_Handler()
        {
            using (var handler = new WindsorHandler(container =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                container.Install(new HandlerInstaller(),
                    WithFeatures.FromAssembly(assembly));
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
    }
}
