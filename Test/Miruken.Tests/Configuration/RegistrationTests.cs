namespace Miruken.Tests.Configuration
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Configuration;
    using Miruken.Register;

    [TestClass]
    public class RegistrationTests
    {
        private IHost _host;
        private IConfiguration _configuration;

        [TestInitialize]
        public void TestInitialize()
        {
            _host = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory());
                    configApp.AddJsonFile("appsettings.json", false);
                    _configuration = configApp.Build();
                }).Build();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _host?.Dispose();
        }

        [TestMethod]
        public void Should_Create_Typed_Configuration()
        {
            var handler = new ServiceCollection()
                .AddMiruken(configure => configure
                    .PublicSources(sources => sources.FromAssemblyOf<RegistrationTests>())
                    .WithTypedConfiguration(_configuration)
                ).Build();

            var configuration = handler.Resolve<MyConfiguration>();
            Assert.AreEqual(2, configuration.Timeout.Hours);
            Assert.AreEqual(30, configuration.Timeout.Minutes);
        }

        [TestMethod]
        public void Should_Create_Named_Typed_Configuration()
        {
            var handler = new ServiceCollection()
                .AddMiruken(configure => configure
                    .PublicSources(sources => sources.FromAssemblyOf<RegistrationTests>())
                    .WithTypedConfiguration(_configuration)
                ).Build();

            var configuration = handler.Resolve<MyNamedConfiguration>();
            Assert.AreEqual("www.google.com", configuration.Url);
        }

        [Configuration]
        public class MyConfiguration
        {
            public TimeSpan Timeout { get; set; }
        }

        [Configuration("Foo")]
        public class MyNamedConfiguration
        {
            public string Url { get; set; }
        }
    }
}

