#if NETSTANDARD
namespace Miruken.Tests.Log
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Callback;
    using Miruken.Callback.Policy.Bindings;
    using Miruken.Concurrency;
    using Miruken.Log;
    using Miruken.Register;
    using NLog;
    using NLog.Config;
    using NLog.Extensions.Logging;
    using NLog.Targets;
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using ServiceCollection = Miruken.Register.ServiceCollection;

    [TestClass]
    public class LoggerProviderTests
    {
        protected LoggingConfiguration _loggingConfig;
        protected MemoryTarget _memoryTarget;
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
            _loggingConfig.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Debug, _memoryTarget));
            LogManager.Configuration = _loggingConfig;

            _handler = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog();
                })
                .AddMiruken(configure => configure
                    .PublicSources(sources => sources.AddTypes(
                        typeof(Inventory), typeof(Billing)))
                )
                .Build();
        }

        [TestMethod]
        public void Should_Inject_Logger_Contextually()
        {
            var handled = _handler.Handle(new Order
            {
                PLU      = "871212",
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
                @".*DEBUG.*Miruken\.Tests\.Log\.LoggerProviderTests\.Billing.*Received invoice for \$150\.30").Success));
        }

        public class Order
        {
            public string PLU      { get; set; }
            public int    Quantity { get; set; }
        }

        public class Invoice
        {
            public decimal Amount { get; set; }
        }

        public class Inventory : Handler
        {
            private readonly ILogger _logger;

            public Inventory(ILogger logger)
            {
                _logger = logger;
            }

            [Handles]
            public void Order(Order order)
            {
                _logger.LogDebug("Received order for {Quantity} '{PLU}'",
                    order.Quantity, order.PLU);
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
            public void Invoice(Invoice invoice, ILogger<Billing> logger)
            {
                Assert.AreSame(_logger, logger);
                _logger.LogDebug("Received invoice for {Amount}",
                    invoice.Amount.ToString("C"));
            }
        }
    }
}
#endif