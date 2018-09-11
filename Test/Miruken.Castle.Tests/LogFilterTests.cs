namespace Miruken.Castle.Tests
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Castle;
    using global::Castle.Facilities.Logging;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.Services.Logging.NLogIntegration;
    using global::Castle.Windsor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NLog;
    using NLog.Config;
    using NLog.Targets;
    using Validate;

    [TestClass]
    public class LogFilterTests
    {
        protected LoggingConfiguration _loggingConfig;
        protected IWindsorContainer _container;
        protected MemoryTarget _memoryTarget;
        private IHandler _handler;
    
        [TestInitialize]
        public void TestInitialize()
        {
            HandlerDescriptor.ResetDescriptors();

            _memoryTarget = new MemoryTarget
            {
                Layout = "${date} [${threadid}] ${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}"
            };
            _loggingConfig = new LoggingConfiguration();
            _loggingConfig.AddTarget("InMemoryTarget", _memoryTarget);
            _loggingConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, _memoryTarget));
            LogManager.Configuration = _loggingConfig;
            _container = new WindsorContainer()
                .AddFacility<LoggingFacility>(f => f.LogUsing(new NLogFactory(_loggingConfig)))
                .Install(new FeaturesInstaller(
                        new HandleFeature().AddFilters(
                                typeof(LogFilter<,>), typeof(ConsoleFilter))
                            .AddMethodFilters(typeof(LogFilter<,>)))
                    .Use(Classes.FromThisAssembly()));
            _container.Kernel.AddHandlersFilter(new ContravariantFilter());

            _handler = new WindsorHandler(_container);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
        }

        [TestMethod]
        public void Should_Log_Callbacks()
        {
            var handled = _handler.Infer().Handle(new Foo());
            Assert.IsTrue(handled);

            var events = _memoryTarget.Logs;
            Assert.AreEqual(4, events.Count);
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Castle\.Tests\.LogFilterTests\+CallbackHandler.*Handling Foo").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Castle\.Tests\.LogFilterTests\+CallbackHandler.*Handling Bar").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Castle\.Tests\.LogFilterTests\+CallbackHandler.*Completed Bar").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Castle\.Tests\.LogFilterTests\+CallbackHandler.*Completed Foo").Success));
        }

        [TestMethod]
        public void Should_Log_Methods()
        {
            var id = _handler
                .Proxy<ResolvingTests.IEmailFeature>()
                .Email("Hello");
            Assert.AreEqual(1, id);

            var events = _memoryTarget.Logs;
            Assert.AreEqual(2, events.Count);
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Castle\.Tests\.ResolvingTests\+EmailHandler.*Handling HandleMethod").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Castle\.Tests\.ResolvingTests\+EmailHandler.*Completed HandleMethod.*with int").Success));
        }

        [TestMethod]
        public void Should_Log_Exceptions_As_Error()
        {
            try
            {
                _handler.Infer().Handle(new Bad(new ArgumentException("Bad")));
                Assert.Fail("Should not get here");
            }
            catch
            {
                var events = _memoryTarget.Logs;
                Assert.AreEqual(2, events.Count);
                Assert.IsTrue(events.Any(x => Regex.Match(x,
                    @"ERROR.*System\.ArgumentException.*Miruken\.Castle\.Tests\.LogFilterTests\+CallbackHandler Failed Bad").Success));
            }
        }

        [TestMethod]
        public void Should_Filter_Exceptions()
        {
             _loggingConfig.LoggingRules.Insert(0, new LoggingRule(
                 "Miruken.Validate.ValidationException", LogLevel.Trace, null)
             {
                 Final = true
             });

            try
            {
                _handler.Infer().Handle(new Bad(
                    new ValidationException(new ValidationOutcome())));
                Assert.Fail("Should not get here");
            }
            catch
            {
                var events = _memoryTarget.Logs;
                Assert.AreEqual(1, events.Count);
                Assert.IsFalse(events.Any(x => Regex.Match(x,
                    @"ERROR.*Miruken\.Validate\.ValidationException.*Miruken\.Castle\.Tests\.LogFilterTests\+CallbackHandler Failed Bad").Success));
            }
        }

        public class Foo { }
        public class Bar { }

        public class Bad
        {
            public Bad(Exception exception)
            {
                Exception = exception;
            }

            public Exception Exception { get; }
        }

        public class CallbackHandler : Handler
        {
            [Handles]
            public void Handle(Foo foo)
            {
            }

            [Handles]
            public void Handle(Bar bar)
            {
            }

            [Handles]
            public void Handle(Bad bad)
            {
                throw bad.Exception;
            }
        }

        public class ConsoleFilter : IFilter<Foo, object>
        {
            public int? Order { get; set; }

            public Task<object> Next(
                Foo callback, MemberBinding member, 
                IHandler composer, Next<object> next,
                IFilterProvider provider = null)
            {
                Console.WriteLine(callback);
                composer.EnableFilters().Handle(new Bar());
                return next();
            }
        }
    }
}
