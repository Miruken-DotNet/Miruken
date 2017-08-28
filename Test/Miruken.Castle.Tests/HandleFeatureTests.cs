namespace Miruken.Castle.Tests
{
    using Callback;
    using Callback.Policy;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using IHandler = Callback.IHandler;

    [TestClass]
    public class HandleFeatureTests
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
                new FeaturesInstaller(new HandleFeature())
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
                    new FeaturesInstaller(new HandleFeature())
                        .Use(Classes.FromThisAssembly()));
            }))
            {
                Assert.IsTrue(handler.Resolve().Handle(new A()));
            } 
        }


        [TestMethod]
        public void Should_Ignore_Filters_With_Unmatched_Constraints()
        {
            _container.Install(
                new FeaturesInstaller(new HandleFeature())
                   .Use(Classes.FromThisAssembly()));
            var handler = new WindsorHandler(_container);
            handler.Resolve().Command(new GetResults());
        }

        [TestMethod]
        public void Should_Use_Filters_With_Matched_Constraints()
        {
            _container.Install(
                new FeaturesInstaller(new HandleFeature())
                   .Use(Classes.FromThisAssembly()));
            var handler = new WindsorHandler(_container);
            var publish = new PublishResults();
            handler.Resolve().Command(publish);
            Assert.IsTrue(publish.Running);
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

        public class Job
        {
            public bool Running { get; set; }
        }

        public class GetResults { }

        public class PublishResults : Job { }

        [Filter(typeof(IFilter<,>))]
        public class GetResultsHandler : Handler
        {
            [Handles]
            public void Get(GetResults get)
            {
            }
        }

        [Filter(typeof(IFilter<,>), Many = true)]
        public class PublishResultsHandler : Handler
        {
            [Handles]
            public void Publish(PublishResults publish)
            {
            }
        }

        public class SingleRunFilter<T> : IFilter<T, object>
            where T : Job
        {
            public int? Order { get; set; }

            public object Next(T job, MethodBinding method,
                IHandler composer, NextDelegate<object> next)
            {
                if (job.Running) return null;
                job.Running = true;
                return next();
            }
        }
    }
}
