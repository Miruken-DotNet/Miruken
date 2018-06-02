namespace Miruken.Security.Tests
{
    using System;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /*
    [TestClass]
    public class AuthorizedAttributeTests
    {
        [TestMethod]
        public void Grants_Access_If_Policy_Approves()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim(identity.RoleClaimType, "manager"));
            var handler = new InventoryHandler()
                        + new AccessControlPolicy()
                        + new FilterHandler();
            var order       = new PlaceOrder("12345");
            var confirmtion = handler
                .Provide(new ClaimsPrincipal(identity))
                .Command<OrderConfirmation>(order);
            Assert.AreEqual("12345", confirmtion.PLU);
        }

        [TestMethod,
         ExpectedException(typeof(UnauthorizedAccessException))]
        public void Rejects_Access_If_Policy_Denies()
        {
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim(identity.RoleClaimType, "cashier"));
            var handler  = new InventoryHandler()
                         + new AccessControlPolicy()
                         + new FilterHandler();
            var order = new PlaceOrder("12345");
            handler.Provide(new ClaimsPrincipal(identity))
                   .Command<OrderConfirmation>(order);
        }

        private class PlaceOrder
        {
            public PlaceOrder(string plu)
            {
                PLU = plu;
            }

            public string PLU { get; }
        }

        private class OrderConfirmation
        {
            public OrderConfirmation(string plu)
            {
                PLU = plu;
            }

            public string PLU { get; }
        }

        private class InventoryHandler : Handler
        {
            [Handles, Authorized("order")]
            public OrderConfirmation Place(PlaceOrder order)
            {
                return new OrderConfirmation(order.PLU);
            }
        }

        private class AccessControlPolicy : Handler, IAccessDecision
        {
            public Task<bool> CanAccess(
                MethodBinding method, IPrincipal principal,
                object scope, IHandler composer)
            {
                var grant = false;
                if (scope is AuthorizedAttribute authorized &&
                    authorized.Policy == "order")
                    grant = principal.IsInRole("manager");
                return Task.FromResult(grant);
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
    */
}
