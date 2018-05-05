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
        public void Should_Use_Most_Specific_Filter()
        {
            _container.Install(
                new FeaturesInstaller(new HandleFeature())
                   .Use(Classes.FromThisAssembly()));
            var handler = new WindsorHandler(_container);
            var clear   = new ClearResults();
            handler.Resolve().Command(clear);
            Assert.AreEqual(-1, clear.Running);
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
            Assert.AreEqual(7, publish.Running);
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
            public int Running { get; set; }
        }

        public class GetResults { }

        public class ClearResults : Job { }

        public class PublishResults : Job { }

        [Filter(typeof(IFilter<,>))]
        public class GetResultsHandler : Handler
        {
            [Handles]
            public void Get(GetResults get)
            {
            }
        }

        [Filter(typeof(IFilter<,>))]
        public class ClearResultsHandler : Handler
        {
            [Handles]
            public void Clear(ClearResults clear)
            {
                clear.Running *= 3;
            }
        }

        [Filter(typeof(IFilter<,>), Many = true)]
        public class PublishResultsHandler : Handler
        {
            [Handles]
            public void Publish(PublishResults publish)
            {
                publish.Running *= 2;
            }
        }

        public class JobFilter : IFilter<Job, object>
        {
            public int? Order { get; set; }

            public object Next(Job job, MethodBinding method,
                IHandler composer, Next<object> next)
            {
                var result = next();
                job.Running--;
                return result;
            }
        }

        public class JobFilter<Res> : IFilter<Job, Res>
        {
            public int? Order { get; set; }

            public Res Next(Job job, MethodBinding method,
                IHandler composer, Next<Res> next)
            {
                job.Running += 3;
                return next();
            }
        }

        public class RunningFilter<Req, Res> : IFilter<Req, Res>
            where Req : Job
        {
            public int? Order { get; set; }

            public Res Next(Req job, MethodBinding method,
                IHandler composer, Next<Res> next)
            {
                job.Running++;
                return next();
            }
        }
    }
}
