namespace Miruken.Http.Tests
{
    using System;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Encodings.Web;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Callback;
    using Format;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Validate;
    using Register;
    using ServiceCollection = Microsoft.Extensions.DependencyInjection.ServiceCollection;

#if NETSTANDARD2_1
    using Microsoft.Extensions.Hosting;
#endif

    public class HttpTestScenario
    {
        private TestServer _server;
        protected IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _server = CreateTestServer();

            _handler = new ServiceCollection()
                .AddSingleton(_server)
                .AddTransient<TestServerClientHandler>()
                .AddMiruken(registration => registration
                    .PublicSources(sources => sources.FromAssemblyOf<HttpTestScenario>())
                    .WithHttp(http => http.AddHttpMessageHandler<TestServerClientHandler>())
                    .WithValidation()
                ).Build()
                .BaseUrl(_server.BaseAddress.AbsoluteUri);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _server?.Dispose();
        }

        protected virtual TestServer CreateTestServer()
        {
            var builder = WebHost.CreateDefaultBuilder().UseStartup<Startup>();
            return new TestServer(builder);
        }

        private class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddMvcCore()
#if NETSTANDARD2_0
                    .AddJsonFormatters(o =>
                    {
                        o.NullValueHandling = NullValueHandling.Ignore;
                        o.TypeNameHandling  = TypeNameHandling.Auto;
                        o.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
                        o.ContractResolver  = new CamelCasePropertyNamesContractResolver();
                        o.Converters.Add(EitherJsonConverter.Instance);
                    })
#elif NETSTANDARD2_1
                    .AddNewtonsoftJson(ns =>
                    {
                        var o = ns.SerializerSettings;
                        o.NullValueHandling = NullValueHandling.Ignore;
                        o.TypeNameHandling  = TypeNameHandling.Auto;
                        o.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
                        o.ContractResolver  = new CamelCasePropertyNamesContractResolver();
                        o.Converters.Add(EitherJsonConverter.Instance);
                    })
#endif
                    .AddFormatterMappings();
            }

            public void Configure(IApplicationBuilder app)
            {
#if NETSTANDARD2_0
                app.UseMvc();
#elif NETSTANDARD2_1
                app.UseRouting()
                   .UseEndpoints(endpoints => endpoints.MapControllers());
#endif
            }
        }
    }

    public class SecureHttpTestScenario : HttpTestScenario
    {
        protected override TestServer CreateTestServer()
        {
            var builder = WebHost.CreateDefaultBuilder().UseStartup<SecureStartup>();
            return new TestServer(builder);
        }

        private class SecureStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddAuthentication("Basic")
                    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

                services.AddAuthorization(options => {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .AddAuthenticationSchemes("Basic")
                        .RequireAuthenticatedUser()
                        .Build();
                });

                services.AddMvcCore()
#if NETSTANDARD2_0
                    .AddJsonFormatters(o =>
                    {
                        o.NullValueHandling = NullValueHandling.Ignore;
                        o.TypeNameHandling = TypeNameHandling.Auto;
                        o.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
                        o.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        o.Converters.Add(EitherJsonConverter.Instance);
                    })
#elif NETSTANDARD2_1
                    .AddNewtonsoftJson(jo =>
                    {
                        var o = jo.SerializerSettings;
                        o.NullValueHandling = NullValueHandling.Ignore;
                        o.TypeNameHandling = TypeNameHandling.Auto;
                        o.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
                        o.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        o.Converters.Add(EitherJsonConverter.Instance);
                    })
#endif
                    .AddFormatterMappings();
            }

#if NETSTANDARD2_0
            public void Configure(IApplicationBuilder app, IHostingEnvironment env)
#elif NETSTANDARD2_1
            public void Configure(IApplicationBuilder app, IHostEnvironment env)
#endif
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

#if NETSTANDARD2_0
                app.UseAuthentication()
                   .UseMvc();
#elif NETSTANDARD2_1
                app.UseRouting()
                   .UseAuthentication()
                   .UseAuthorization()
                   .UseEndpoints(endpoints => endpoints.MapControllers());
#endif
            }
        }

        private class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public BasicAuthenticationHandler(
                IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger,
                UrlEncoder encoder,
                ISystemClock clock)
                : base(options, logger, encoder, clock)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var authHeader      = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials     = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username        = credentials[0];
                var claims          = new[] { new Claim(ClaimTypes.Name, username),  };
                var identity        = new ClaimsIdentity(claims, Scheme.Name);
                var principal       = new ClaimsPrincipal(identity);
                var ticket          = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }
    }
}

