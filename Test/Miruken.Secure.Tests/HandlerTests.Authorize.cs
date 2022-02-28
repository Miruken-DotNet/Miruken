namespace Miruken.Secure.Tests;

using System;
using System.Security.Claims;
using System.Security.Principal;
using Callback;
using Callback.Policy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class HandlerAuthorizeTests
{
    [TestInitialize]
    public void TestInitialize()
    {
        var factory = new MutableHandlerDescriptorFactory();
        factory.RegisterDescriptor<TransferFundsAccessPolicy>();
        factory.RegisterDescriptor<Provider>();
        HandlerDescriptorFactory.UseFactory(factory);
    }

    [TestMethod]
    public void Grants_If_No_Authenticated_Principal()
    {
        var principal = new GenericPrincipal(
            new GenericIdentity(""), Array.Empty<string>());
        var grant     = new Handler().Authorize(new TransferFunds(1000M), principal);
        Assert.IsTrue(grant);
    }

    [TestMethod]
    public void Denies_If_No_Authenticated_Principal_And_Required()
    {
        var principal = new GenericPrincipal(
            new GenericIdentity(""), Array.Empty<string>());
        var grant     = new Handler()
            .RequireAuthentication()
            .Authorize(new TransferFunds(1000M), principal);
        Assert.IsFalse(grant);
    }

    [TestMethod]
    public void Grants_If_No_Access_Policy()
    {
        var principal = new GenericPrincipal(
            new GenericIdentity("test"), Array.Empty<string>());
        var grant     = new Handler().Authorize(new TransferFunds(1000M), principal);
        Assert.IsTrue(grant);
    }

    [TestMethod]
    public void Denies_If_No_Access_Policy_And_Required()
    {
        var principal = new GenericPrincipal(
            new GenericIdentity("test"), Array.Empty<string>());
        var grant     = new Handler()
            .RequireAccess()
            .Authorize(new TransferFunds(1000M), principal);
        Assert.IsFalse(grant);
    }

    [TestMethod]
    public void Grants_If_Access_Policy_Accepts()
    {
        var principal = new GenericPrincipal(
            new GenericIdentity("test"), Array.Empty<string>());
        var grant     = new TransferFundsAccessPolicy()
            .RequireAccess()
            .Authorize(new TransferFunds(1000M), principal);
        Assert.IsTrue(grant);
    }

    [TestMethod]
    public void Grants_If_Access_Policy_Accepts_Required_Role()
    {
        var identity  = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, "manager"));
        var principal = new GenericPrincipal(identity, Array.Empty<string>());
        var grant     = new TransferFundsAccessPolicy()
            .RequireAccess()
            .Authorize(new TransferFunds(1000000M), principal);
        Assert.IsTrue(grant);
    }

    [TestMethod]
    public void Denies_If_Access_Policy_Rejects()
    {
        var principal = new GenericPrincipal(
            new GenericIdentity("test"), Array.Empty<string>());
        var grant     = new TransferFundsAccessPolicy()
            .RequireAccess()
            .Authorize(new TransferFunds(1000000M), principal);
        Assert.IsFalse(grant);
    }

    private class TransferFunds
    {
        public decimal Amount { get; }

        public TransferFunds(decimal amount)
        {
            Amount = amount;
        }
    }

    private class TransferFundsAccessPolicy : Handler
    {
        [Authorizes]
        public bool Authorize(
            TransferFunds transfer, IPrincipal principal)
        {
            var claims = principal.RequireAuthenticatedClaims();
            return transfer.Amount < 10000M || claims.HasRole("manager");
        }
    }
}