namespace Miruken.Secure.Tests
{
    using System;
    using System.Security.Claims;
    using System.Security.Principal;
    using Callback;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ClaimAttributeTests
    {
        private ClaimsIdentity _identity;
        private IPrincipal _principal;

        [TestInitialize]
        public void TestInitialize()
        {
            _identity  = new ClaimsIdentity(Array.Empty<Claim>(), "test");
            _principal = new ClaimsPrincipal(_identity);
        }

        [TestMethod]
        public void Converts_Claim_Into_Simple_Type()
        {
            _identity.AddClaim(new Claim(ClaimTypes.Name, "James N. Mattis"));
            var handler = new MissileControl().Provide(_principal);
            Assert.IsTrue(handler.Handle(new LaunchMissile()));
        }

        [TestMethod]
        public void Converts_Claim_Into_Guid()
        {
            _identity.AddClaim(new Claim("code",
                "4402DCD9-980B-426C-B9BD-F06A0798AE56"));
            var handler = new MissileControl().Provide(_principal);
            Assert.IsTrue(handler.Handle(new AbortMissileLaunch()));
        }

        [TestMethod]
        public void Rejects_Callback_If_Missing_Claim()
        {
            var handler = new MissileControl().Provide(_principal);
            Assert.IsFalse(handler.Handle(new LaunchMissile()));
        }
        
        private class LaunchMissile { }

        private class AbortMissileLaunch { }

        private class MissileControl : Handler
        {
            [Handles]
            public void Launch(LaunchMissile launch,
                [Claim(ClaimTypes.Name)] string who)
            {
                Console.WriteLine($@"{who} launched the Missile");
            }

            [Handles]
            public void Abort(AbortMissileLaunch abort,
                [Claim("code")] Guid code)
            {
                Assert.AreEqual(Guid.Parse(
                    "4402DCD9-980B-426C-B9BD-F06A0798AE56"), code);
            }
        }
    }
}
