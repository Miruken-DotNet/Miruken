namespace Miruken.Validate.Tests
{
    using System;
    using System.Linq;
    using Callback;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Model;
    using Register;

    [TestClass]
    public class RegistrationTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _handler = new ServiceCollection()
                .AddMiruken(configure => configure
                    .PublicSources(sources => sources.FromAssemblyOf<RegistrationTests>())
                    .WithValidation()
                ).Build();
        }

        [TestMethod]
        public void Should_Validate_Target()
        {
            var player = new Player
            {
                DOB = new DateTime(2007, 6, 14)
            };
            var outcome = _handler.Validate(player);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, player.ValidationOutcome);
            CollectionAssert.AreEquivalent(outcome.Culprits, new [] { "FirstName", "LastName" });
            CollectionAssert.AreEquivalent(
                outcome.GetErrors("FirstName").Cast<string>().ToArray(),
                new []
                {
                    "First name is required",
                    "The FirstName field is required.",
                    "'First Name' must not be empty."
                });
            CollectionAssert.AreEquivalent(
                outcome.GetErrors("LastName").Cast<string>().ToArray(),
                new[]
                {
                    "Last name is required",
                    "The LastName field is required.",
                    "'Last Name' must not be empty."
                });
        }
    }
}
