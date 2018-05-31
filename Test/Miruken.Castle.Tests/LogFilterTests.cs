﻿namespace Miruken.Castle.Tests
{
    using System.Linq;
    using System.Text.RegularExpressions;
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

    [TestClass]
    public class LogFilterTests
    {
        protected IWindsorContainer _container;
        protected MemoryTarget _memoryTarget;
        private IHandler _handler;
    
        [TestInitialize]
        public void TestInitialize()
        {
            HandlerDescriptor.ResetDescriptors();

            var config = new LoggingConfiguration();
            _memoryTarget = new MemoryTarget
            {
                Layout = "${date} [${threadid}] ${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}"
            };
            config.AddTarget("InMemoryTarget", _memoryTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, _memoryTarget));
            LogManager.Configuration = config;
            _container = new WindsorContainer()
                .AddFacility<LoggingFacility>(f => f.LogUsing(new NLogFactory(config)))
                .Install(new FeaturesInstaller(
                        new HandleFeature().AddMethodFilters(typeof(LogFilter<,>)))
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
    }
}