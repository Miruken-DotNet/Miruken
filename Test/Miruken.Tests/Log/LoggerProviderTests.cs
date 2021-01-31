namespace Miruken.Tests.Log
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Register;
    using NLog;
    using NLog.Config;
    using NLog.Extensions.Logging;
    using NLog.Targets;
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using LogLevel = NLog.LogLevel;

    [TestClass]
    public class LoggerProviderTests
    {
        private LoggingConfiguration _loggingConfig;
        private MemoryTarget _memoryTarget;
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _memoryTarget = new MemoryTarget
            {
                Layout = "${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}"
            };
            _loggingConfig = new LoggingConfiguration();
            _loggingConfig.AddTarget("InMemoryTarget", _memoryTarget);
            _loggingConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, _memoryTarget));
            LogManager.Configuration = _loggingConfig;

            _handler = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog();
                })
                .AddMiruken(configure => configure.Sources(
                    sources => sources.AddTypes(typeof(Inventory), typeof(Billing)))
                )
                .Build();
        }

        [TestMethod]
        public void Should_Inject_Logger_Contextually()
        {
            var handled = _handler.Handle(new Order
            {
                Plu      = "871212",
                Quantity = 3
            });
            Assert.IsTrue(handled);

            var events = _memoryTarget.Logs;
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @".*DEBUG.*Miruken\.Tests\.Log\.LoggerProviderTests\.Inventory.*Received order for 3 '871212'").Success));
        }

        [TestMethod]
        public void Should_Inject_Logger_Explicitly()
        {
            var handled = _handler.Handle(new Invoice { Amount = 150.30M });

            Assert.IsTrue(handled);

            var events = _memoryTarget.Logs;
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @".*DEBUG.*Miruken\.Tests\.Log\.LoggerProviderTests\.Billing.*Received invoice for.*150\.30").Success));
        }

        private class Order
        {
            public string Plu      { get; set; }
            public int    Quantity { get; set; }
        }

        public class Invoice
        {
            public decimal Amount { get; set; }
        }

        private class Inventory : Handler
        {
            private readonly ILogger _logger;

            public Inventory(ILogger logger)
            {
                _logger = logger;
            }

            [Handles]
            public void Receive(Order order)
            {
                _logger.LogDebug("Received order for {Quantity} '{PLU}'",
                    order.Quantity, order.Plu);
            }
        }

        public class Billing : Handler
        {
            private readonly ILogger<Billing> _logger;

            public Billing(ILogger<Billing> logger)
            {
                _logger = logger;
            }
            
            [Handles]
            public void Process(Invoice invoice, ILogger<Billing> logger)
            {
                Assert.AreSame(_logger, logger);
                _logger.LogDebug("Received invoice for {Amount}",
                    invoice.Amount.ToString("C"));
            }
        }
    }
}