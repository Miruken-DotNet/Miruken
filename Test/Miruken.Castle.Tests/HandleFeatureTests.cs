namespace Miruken.Castle.Tests
{
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Infrastructure;
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
            var handler = new WindsorHandler(_container);
            Assert.IsNotNull(handler.Resolve<MyHandler>());
            Assert.IsTrue(handler.Resolve().Handle(new A()));
        }

        [TestMethod]
        public void Should_Exclude_Handlers()
        {
            _container.Install(
                new FeaturesInstaller(new HandleFeature()
                        .ExcludeHandlers(type => type.Is<MyHandler>()))
                    .Use(Classes.FromThisAssembly()));
            var handler = new WindsorHandler(_container);
            Assert.IsNull(handler.Resolve<MyHandler>());
            Assert.IsFalse(handler.Resolve().Handle(new A()));
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

            public async Task<object> Next(Job job, MemberBinding member,
                IHandler composer, Next<object> next,
                IFilterProvider provider)
            {
                var result = await next();
                job.Running--;
                return result;
            }
        }

        public class JobFilter<Res> : IFilter<Job, Res>
        {
            public int? Order { get; set; }

            public Task<Res> Next(Job job, MemberBinding member,
                IHandler composer, Next<Res> next,
                IFilterProvider provider)
            {
                job.Running += 3;
                return next();
            }
        }

        public class RunningFilter<Req, Res> : IFilter<Req, Res>
            where Req : Job
        {
            public int? Order { get; set; }

            public Task<Res> Next(Req job, MemberBinding member,
                IHandler composer, Next<Res> next,
                IFilterProvider provider)
            {
                job.Running++;
                return next();
            }
        }
    }
}
