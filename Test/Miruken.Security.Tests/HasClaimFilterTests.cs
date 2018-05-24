namespace Miruken.Security.Tests
{
    using System;
    using System.Security.Claims;
    using System.Threading;
    using Callback;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HasClaimFilterTests
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
        public void Rejects_Callback_If_Missing_Claim()
        {
            var handler = new MissileControlHandler()
                        + new FilterHandler();
            var launch  = new LaunchMissile("Patriot");
            handler.Provide(Thread.CurrentPrincipal)
                .Command<LaunchConfirmation>(launch);
        }

        [TestMethod]
        public void Handles_Callback_If_Claim_Satisfied()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("scope", "launch"));
            var handler  = new MissileControlHandler()
                         + new FilterHandler();
            var launch      = new LaunchMissile("Patriot");
            var confirmtion = handler
                .Provide(new ClaimsPrincipal(identity))
                .Command<LaunchConfirmation>(launch);
            Assert.AreEqual("Patriot", confirmtion.Missle);
        }

        [TestMethod]
        public void Handles_Callback_If_Role_Satisfied()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("scope", "launch"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "president"));
            var handler  = (new MissileControlHandler()
                         + new FilterHandler()).Provide(
                new ClaimsPrincipal(identity));
            var launch      = new LaunchMissile("Scud");
            var confirmtion = handler
                .Command<LaunchConfirmation>(launch);
            var abortLaunch = new AbortLaunch(confirmtion);
            var aborted     = handler.Command<LaunchAborted>(abortLaunch);
            Assert.AreEqual("Scud", aborted.Launch.Missle);
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

        private class MissileControlHandler : Handler
        {
            [Handles,
             HasClaim("scope", "launch")]
            public LaunchConfirmation Launch(LaunchMissile launch)
            {
                return new LaunchConfirmation(launch.Missle);
            }

            [Handles,
             HasRole("president")]
            public LaunchAborted Abort(AbortLaunch abort)
            {
                return new LaunchAborted(abort.Launch);
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
