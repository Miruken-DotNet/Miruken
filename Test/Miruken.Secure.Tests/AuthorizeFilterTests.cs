// ReSharper disable UnusedMember.Local
namespace Miruken.Secure.Tests;

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Callback;
using Callback.Policy;
using Concurrency;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AuthorizeFilterTests
{
    [TestInitialize]
    public void TestInitialize()
    {
        var factory = new MutableHandlerDescriptorFactory();
        factory.RegisterDescriptor<MissileControlHandler>();
        factory.RegisterDescriptor<MissileAccessPolicy>();
        factory.RegisterDescriptor<FilterHandler>();
        factory.RegisterDescriptor<Provider>();
        HandlerDescriptorFactory.UseFactory(factory);
    }

    [TestMethod,
     ExpectedException(typeof(NotSupportedException))]
    public void Rejects_Callback_If_Filter_Not_Resolved()
    {
        var handler = new MissileControlHandler();
        var launch  = new LaunchMissile("Patriot");
        handler.Command<LaunchConfirmation>(launch);
    }

    [TestMethod,
     ExpectedException(typeof(NotSupportedException))]
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
        handler.Provide(new ClaimsPrincipal(
                new ClaimsIdentity(Array.Empty<Claim>(), "test")))
            .Command<LaunchConfirmation>(launch);
    }

    [TestMethod,
     ExpectedException(typeof(UnauthorizedAccessException))]
    public void Rejects_Callback_If_Claim_Unsatisfied()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("scope", "test"));
        var handler = new MissileControlHandler()
                      + new MissileAccessPolicy()
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
                       + new MissileAccessPolicy()
                       + new FilterHandler();
        var launch       = new LaunchMissile("Patriot");
        var confirmation = handler
            .Provide(new ClaimsPrincipal(identity))
            .Command<LaunchConfirmation>(launch);
        Assert.AreEqual("Patriot", confirmation.Missile);
    }

    [TestMethod]
    public async Task Handles_Callback_If_Role_Satisfied()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("scope", "launch"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "president"));
        var handler  = (new MissileControlHandler()
                        + new MissileAccessPolicy()
                        + new FilterHandler()).Provide(
            new ClaimsPrincipal(identity));
        var launch       = new LaunchMissile("Scud");
        var confirmation = handler
            .Command<LaunchConfirmation>(launch);
        var abortLaunch = new AbortLaunch(confirmation);
        var aborted     = await handler.CommandAsync<LaunchAborted>(abortLaunch);
        Assert.AreEqual("Scud", aborted.Launch.Missile);
    }

    [TestMethod]
    public void Handles_Callback_Using_Default_Policy()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, "tester"));
        var handler = new MissileControlHandler()
                      + new MissileAccessPolicy()
                      + new FilterHandler();
        var test = new TestMissile("Tomahawk");
        var confirmation = handler
            .Provide(new ClaimsPrincipal(identity))
            .Command<MissileReport>(test);
        Assert.AreEqual("Tomahawk", confirmation.Missile);
        Assert.IsTrue(confirmation.Passed);
    }

    [TestMethod]
    public void Handles_Implicit_Protocol_If_Claim_Satisfied()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("scope", "shutdown"));
        var handler = new MissileControlHandler()
                      + new MissileAccessPolicy()
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
                       + new MissileAccessPolicy()
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
                      + new MissileAccessPolicy()
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
                       + new MissileAccessPolicy()
                       + new FilterHandler();
        handler.Provide(new ClaimsPrincipal(identity))
            .Proxy<IControl>().EnterDefcon(3);
    }

    private class TestMissile
    {
        public string Missile { get; }

        public TestMissile(string missile)
        {
            Missile = missile;
        }
    }

    public class MissileReport
    {
        public string Missile { get; }
        public bool   Passed { get; }

        public MissileReport(string missile, bool passed)
        {
            Missile = missile;
            Passed  = passed;
        }
    }

    private class LaunchMissile
    {
        public string Missile { get; }

        public LaunchMissile(string missile)
        {
            Missile = missile;
        }
    }

    public class LaunchConfirmation
    {
        public string Missile { get; }

        public LaunchConfirmation(string missile)
        {
            Missile = missile;
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
        public MissileReport Test(TestMissile test)
        {
            return new MissileReport(test.Missile, true);
        }

        [Handles]
        public LaunchConfirmation Launch(LaunchMissile launch)
        {
            return new LaunchConfirmation(launch.Missile);
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

    private class MissileAccessPolicy : Handler
    {
        [Authorizes]
        public bool Authorize(LaunchMissile launch, IPrincipal principal)
        {
            return principal
                .RequireAuthenticatedClaims()
                .HasScope("launch");
        }

        [Authorizes]
        public Promise<bool> Authorize(AbortLaunch abort, IPrincipal principal)
        {
            return Promise.Resolved(
                principal.RequireAuthenticatedClaims()
                    .HasRole("president"));
        }

        [Authorizes("defcon")]
        public bool CanUpdateDefcon(HandleMethod method, IPrincipal principal)
        {
            return principal
                .RequireAuthenticatedClaims()
                .HasScope("defcon");
        }

        [Authorizes("Miruken.Secure.Tests.AuthorizeFilterTests+MissileControlHandler:Shutdown")]
        public bool CanShutdown(HandleMethod method, IPrincipal principal)
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
        public AuthorizeFilter<TCb, TRes> Create<TCb, TRes>() => new();
    }
}