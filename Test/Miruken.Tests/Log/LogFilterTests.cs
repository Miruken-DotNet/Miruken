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
    using ServiceCollection = Miruken.Register.ServiceCollection;

    [TestClass]
    public class LogFilterTests
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
            _loggingConfig.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Debug, _memoryTarget));
            LogManager.Configuration = _loggingConfig;

            _handler = new ServiceCollection()
                .AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                    logging.AddNLog();
                })
                .AddMiruken(configure => configure
                    .PublicSources(sources => sources.AddTypes(
                        typeof(CallbackHandler), typeof(BazHandler), 
                        typeof(StockMarket), typeof(ConsoleFilter)))
                ).Build();
        }

        [TestMethod]
        public void Should_Log_Callbacks()
        {
            var handled = _handler.Handle(new Foo());
            Assert.IsTrue(handled);

            var events = _memoryTarget.Logs;
            Assert.AreEqual(4, events.Count);
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.CallbackHandler.*Handling Foo").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.CallbackHandler.*Handling Bar").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.CallbackHandler.*Completed Bar").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.CallbackHandler.*Completed Foo").Success));
        }

        [TestMethod]
        public async Task Should_Log_Unhandled_Callbacks()
        {
            await _handler.Chain(new BazHandler()).Send(new Baz());

            var events = _memoryTarget.Logs;
            Assert.AreEqual(4, events.Count);
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.CallbackHandler.*Handling Baz").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"ERROR.*System.NotSupportedException Miruken\.Tests\.Log\.LogFilterTests\+CallbackHandler Failed Baz.*System\.NotSupportedException: Miruken\.Concurrency\.Promise Handle\(Baz\) not handled").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.BazHandler.*Handling Baz").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.BazHandler.*Completed Baz").Success));
        }

        [TestMethod]
        public void Should_Log_Methods()
        {
            var cost = _handler.Proxy<IStockMarket>().Buy("MSFT", 3);
            Assert.AreEqual(426, cost);

            var events = _memoryTarget.Logs;
            Assert.AreEqual(2, events.Count);
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.StockMarket.*Handling HandleMethod").Success));
            Assert.IsTrue(events.Any(x => Regex.Match(x,
                @"DEBUG.*Miruken\.Tests\.Log\.LogFilterTests\.StockMarket.*Completed HandleMethod.*with decimal").Success));
        }

        [TestMethod]
        public void Should_Log_Exceptions_As_Error()
        {
            try
            {
                _handler.Handle(new Bad(new ArgumentException("Bad")));
                Assert.Fail("Should not get here");
            }
            catch
            {
                var events = _memoryTarget.Logs;
                Assert.AreEqual(2, events.Count);
                Assert.IsTrue(events.Any(x => Regex.Match(x,
                    @"ERROR.*System\.ArgumentException.*Miruken\.Tests\.Log\.LogFilterTests\+CallbackHandler Failed Bad").Success));
            }
        }

        [TestMethod]
        public void Should_Filter_Exceptions()
        {
             _loggingConfig.LoggingRules.Insert(0, new LoggingRule(
                 "System.InvalidOperationException", NLog.LogLevel.Trace, null)
             {
                 Final = true
             });

            try
            {
                _handler.Handle(new Bad(new InvalidOperationException("Something crashed")));
                Assert.Fail("Should not get here");
            }
            catch
            {
                var events = _memoryTarget.Logs;
                Assert.AreEqual(1, events.Count);
                Assert.IsFalse(events.Any(x => Regex.Match(x,
                    @"ERROR.*System\.InvalidOperationException.*Miruken\.Tests\.Log\.LogFilterTests\+CallbackHandler Failed Bad").Success));
            }
        }

        public class Foo { }
        public class Bar { }
        public class Baz { }

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
            [Handles, Log, Filter(typeof(ConsoleFilter))]
            public void Handle(Foo foo)
            {
            }

            [Handles, Log, Filter(typeof(ConsoleFilter))]
            public void Handle(Bar bar)
            {
            }

            [Handles, Log, Filter(typeof(ConsoleFilter))]
            public Promise Handle(Baz baz) => null;

            [Handles, Log, Filter(typeof(ConsoleFilter))]
            public void Handle(Bad bad) => throw bad.Exception;
        }

        [Unmanaged]
        public class BazHandler : Handler
        {
            [Handles, Log, Filter(typeof(ConsoleFilter))]
            public void Handle(Baz baz)
            {
            }
        }

        public interface IStockMarket : IResolving
        {
            decimal Buy(string symbol, int quantity);
        }

        public class StockMarket : Handler, IStockMarket
        {
            [Log]
            public decimal Buy(string symbol, int quantity) => 142 * quantity;
        }

        public class ConsoleFilter : IFilter<Foo, object>
        {
            public int? Order { get; set; }

            public Task<object> Next(Foo callback,
                object rawCallback, MemberBinding member, 
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