#if NETFULL
namespace Miruken.Http.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Callback;
    using Castle;
    using Functional;
    using global::Castle.MicroKernel.Registration;
    using Get;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Owin;
    using Microsoft.Owin.Hosting;
    using Thinktecture.IdentityModel.Owin;
    using Validate.FluentValidation;

    public class HttpTestScenario
    {
        protected WindsorHandler _container;
        protected IHandler _handler;
        private   IDisposable _server;

        [TestInitialize]
        public void TestInitialize()
        {
            _container = new WindsorHandler(container =>
            {
                container.Install(new FeaturesInstaller(new HandleFeature())
                    .Use(Classes.FromAssemblyContaining<GetHandler>()));
            });

            _server = WebApp.Start("http://localhost:9000/", Configuration);

            _handler = (_container + new FluentValidationValidator())
                .BaseUrl("http://localhost:9000");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _server.Dispose();
            _container.Dispose();
        }

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.Formatters.Clear();
            config.Formatters.Add(HttpFormatters.Route);
            config.MapHttpAttributeRoutes();
            app.UseWebApi(config);
        }

        public void Secure(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.Formatters.Clear();
            config.Formatters.Add(HttpFormatters.Route);
            config.MapHttpAttributeRoutes();
            app.UseBasicAuthentication(
                new BasicAuthenticationOptions("SecureApi", Authenticate));
            app.UseWebApi(config);
        }

        protected virtual Task<IEnumerable<Claim>> Authenticate(
            string username, string password)
        {
            return Task.FromResult(Enumerable
                .Repeat(new Claim(username, password), 1));
        }
    }
}
#endif