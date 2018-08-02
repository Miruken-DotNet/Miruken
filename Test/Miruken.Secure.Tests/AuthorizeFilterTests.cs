namespace Miruken.Secure.Tests
{
    using System;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using Callback;
    using Concurrency;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AuthorizeFilterTests
    {
        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Rejects_Callback_If_Filter_Not_Resolved()
        {
            var handler = new MissileControlHandler();
            var launch  = new LaunchMissile("Patriot");
            handler.Command<LaunchConfirmation>(launch);
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Rejects_Callback_If_No_Principal()
        {
            var handler = new MissileControlHandler()
                        + new FilterHandler();
            var launch  = new LaunchMissile("Patriot");
            handler.Command<LaunchConfirmation>(launch);
        }

        [TestMethod,
         ExpectedException(typeof(UnauthorizedAccessException))]
        public void Rejects_Callback_If_Missing_Policy()
        {
            var handler = new MissileControlHandler()
                        + new FilterHandler();
            var launch  = new LaunchMissile("Patriot");
            handler.Provide(Thread.CurrentPrincipal)
                .Command<LaunchConfirmation>(launch);
        }

        [TestMethod,
         ExpectedException(typeof(UnauthorizedAccessException))]
        public void Rejects_Callback_If_Claim_Unsatisfied()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("scope", "test"));
            var handler = new MissileControlHandler()
                        + new MissleAccessPolicy()
                        + new FilterHandler();
            var launch = new LaunchMissile("Patriot");
            handler.Provide(new ClaimsPrincipal(identity))
                .Command<LaunchConfirmation>(launch);
        }

        [TestMethod]
        public void Handles_Callback_If_Claim_Satisfied()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("scope", "launch"));
            var handler  = new MissileControlHandler()
                         + new MissleAccessPolicy()
                         + new FilterHandler();
            var launch      = new LaunchMissile("Patriot");
            var confirmtion = handler
                .Provide(new ClaimsPrincipal(identity))
                .Command<LaunchConfirmation>(launch);
            Assert.AreEqual("Patriot", confirmtion.Missle);
        }

        [TestMethod]
        public async Task Handles_Callback_If_Role_Satisfied()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("scope", "launch"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "president"));
            var handler  = (new MissileControlHandler()
                         + new MissleAccessPolicy()
                         + new FilterHandler()).Provide(
                new ClaimsPrincipal(identity));
            var launch      = new LaunchMissile("Scud");
            var confirmtion = handler
                .Command<LaunchConfirmation>(launch);
            var abortLaunch = new AbortLaunch(confirmtion);
            var aborted     = await handler.CommandAsync<LaunchAborted>(abortLaunch);
            Assert.AreEqual("Scud", aborted.Launch.Missle);
        }

        [TestMethod]
        public void Handles_Callback_Using_Default_Policy()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim(ClaimTypes.Role, "tester"));
            var handler = new MissileControlHandler()
                        + new MissleAccessPolicy()
                        + new FilterHandler();
            var test = new TestMissile("Tomahawk");
            var confirmtion = handler
                .Provide(new ClaimsPrincipal(identity))
                .Command<MissleReport>(test);
            Assert.AreEqual("Tomahawk", confirmtion.Missle);
            Assert.IsTrue(confirmtion.Passed);
        }

        [TestMethod]
        public void Handles_Implicit_Protocol_If_Claim_Satisfied()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("scope", "shutdown"));
            var handler = new MissileControlHandler()
                        + new MissleAccessPolicy()
                        + new FilterHandler();
            handler.Provide(new ClaimsPrincipal(identity))
                .Proxy<IControl>().Shutdown();
        }

        [TestMethod,
         ExpectedException(typeof(UnauthorizedAccessException))]
        public void Rejects_Implicit_Protocol_If_Claim_Unsatisfied()
        {
            var identity = new ClaimsIdentity("test");
            var handler  = new MissileControlHandler()
                         + new MissleAccessPolicy()
                         + new FilterHandler();
            handler.Provide(new ClaimsPrincipal(identity))
                .Proxy<IControl>().Shutdown();
        }

        [TestMethod]
        public void Handles_Explicit_Protocol_If_Claim_Satisfied()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("scope", "defcon"));
            var handler = new MissileControlHandler()
                        + new MissleAccessPolicy()
                        + new FilterHandler();
            var level = handler.Provide(new ClaimsPrincipal(identity))
                .Proxy<IControl>().EnterDefcon(3);
            Assert.AreEqual(3, level);
        }

        [TestMethod,
         ExpectedException(typeof(UnauthorizedAccessException))]
        public void Rejects_Explicit_Protocol_If_Claim_Satisfied()
        {
            var identity = new ClaimsIdentity("test");
            var handler  = new MissileControlHandler()
                         + new MissleAccessPolicy()
                         + new FilterHandler();
            handler.Provide(new ClaimsPrincipal(identity))
                .Proxy<IControl>().EnterDefcon(3);
        }

        private class TestMissile
        {
            public string Missle { get; }

            public TestMissile(string missle)
            {
                Missle = missle;
            }
        }

        public class MissleReport
        {
            public string Missle { get; }
            public bool   Passed { get; }

            public MissleReport(string missle, bool passed)
            {
                Missle = missle;
                Passed = passed;
            }
        }

        private class LaunchMissile
        {
            public string Missle { get; }

            public LaunchMissile(string missle)
            {
                Missle = missle;
            }
        }

        public class LaunchConfirmation
        {
            public string Missle { get; }

            public LaunchConfirmation(string missle)
            {
                Missle = missle;
            }
        }

        private class AbortLaunch
        {
            public LaunchConfirmation Launch { get; }

            public AbortLaunch(LaunchConfirmation launch)
            {
                Launch = launch;
            }
        }

        public class LaunchAborted
        {
            public LaunchConfirmation Launch { get; }

            public LaunchAborted(LaunchConfirmation launch)
            {
                Launch = launch;
            }
        }

        public interface IControl : IProtocol
        {
            int EnterDefcon(int level);
            void Shutdown();
        }

        [Authorize]
        private class MissileControlHandler : Handler, IControl
        {
            [Handles]
            public MissleReport Test(TestMissile test)
            {
                return new MissleReport(test.Missle, true);
            }

            [Handles]
            public LaunchConfirmation Launch(LaunchMissile launch)
            {
                return new LaunchConfirmation(launch.Missle);
            }

            [Handles]
            public Task<LaunchAborted> Abort(AbortLaunch abort)
            {
                return Task.FromResult(new LaunchAborted(abort.Launch));
            }

            [AccessPolicy("defcon")]
            int IControl.EnterDefcon(int level)
            {
                return level;
            }

            public void Shutdown()
            {
            }
        }

        private class MissleAccessPolicy : Handler
        {
            [Authorizes]
            public bool Authorize(
                LaunchMissile launch, IPrincipal principal)
            {
                return principal
                    .RequireAuthenticatedClaims()
                    .HasScope("launch");
            }

            [Authorizes]
            public Promise<bool> Authorize(
                AbortLaunch abort, IPrincipal principal)
            {
                return Promise.Resolved(
                    principal.RequireAuthenticatedClaims()
                        .HasRole("president"));
            }

            [Authorizes("defcon")]
            public bool CanUpdateDefcon(
                HandleMethod method, IPrincipal principal)
            {
                return principal
                    .RequireAuthenticatedClaims()
                    .HasScope("defcon");
            }

            [Authorizes("Miruken.Secure.Tests.AuthorizeFilterTests+MissileControlHandler:Shutdown")]
            public bool CanShutdown(
                HandleMethod method, IPrincipal principal)
            {
                return principal
                    .RequireAuthenticatedClaims()
                    .HasScope("shutdown");
            }

            [Authorizes]
            public bool Authorize(Authorization authorization)
            {
                if (authorization.Target is TestMissile)
                    return authorization.Principal
                        .RequireAuthenticatedClaims()
                        .HasRole("tester");
                return false;
            }
        }

        private class FilterHandler : Handler
        {
            [Provides]
            public AuthorizeFilter<TCb, TRes> Create<TCb, TRes>()
            {
                return new AuthorizeFilter<TCb, TRes>();
            }
        }
    }
}
