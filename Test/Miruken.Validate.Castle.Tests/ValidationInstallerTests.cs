namespace Miruken.Validate.Castle.Tests
{
    using System;
    using System.Threading.Tasks;
    using FluentValidation;
    using global::Castle.Windsor;
    using global::FluentValidation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Castle;
    using Validate.Tests;
    using Validate.Tests.Model;
    using static Protocol;

    [TestClass]
    public class ValidationInstallerTests
    {
        protected IWindsorContainer _container;
        protected WindsorHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _container = new WindsorContainer()
                .Install(new Plugins(Plugin.FromAssembly(
                    typeof(FluentValidationValidatorTests).Assembly)),
                new ValidationInstaller());
            _container.Kernel.AddHandlersFilter(new ContravariantFilter());
            _handler = new WindsorHandler(_container);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _container.Dispose();
        }

        [TestMethod]
        public void Should_Register_Validators()
        {
            Assert.IsNotNull(_container.Resolve<IValidator<Person>>());
            Assert.IsNotNull(_container.Resolve<IValidator<Player>>());
            Assert.IsNotNull(_container.Resolve<IValidator<Coach>>());
            Assert.IsNotNull(_container.Resolve<IValidator<Team>>());
        }

        [TestMethod]
        public async Task Should_Validate_Target()
        {
            var handler = new ValidationHandler()
                        + new FluentValidationValidator()
                        + _handler;
            var player  = new Player();
            var outcome = await P<IValidating>(handler).ValidateAsync(player);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, player.ValidationOutcome);
            Assert.AreEqual("'First Name' should not be empty.", outcome["FirstName"]);
            Assert.AreEqual("'Last Name' should not be empty.", outcome["LastName"]);
            Assert.AreEqual("'DOB' must not be empty.", outcome["DOB"]);
        }

        [TestMethod]
        public async Task Should_Compose_Validation()
        {
            var handler = new ValidationHandler()
                        + new FluentValidationValidator()
                        + _handler;
            var team = new Team
            {
                Name = "Arsenal",
                Coach = new Coach(),
                Players = new[]
                {
                    new Player(),
                    new Player
                    {
                        FirstName = "Alexis",
                        LastName  = "Sanchez",
                        DOB       = new DateTime(1988, 12, 19)
                    },
                    new Player
                    {
                        FirstName = "Wayne",
                        DOB       = new DateTime(1985, 10, 24)
                    }
                }
            };

            var outcome = await P<IValidating>(handler).ValidateAsync(team);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, team.ValidationOutcome);
            CollectionAssert.AreEquivalent(new[] { "Coach", "Players" }, outcome.Culprits);
            Assert.AreEqual("'First Name' should not be empty.", outcome["Coach.FirstName"]);
            Assert.AreEqual("'Last Name' should not be empty.", outcome["Coach.LastName"]);
            Assert.AreEqual("'First Name' should not be empty.", outcome["Players[0].FirstName"]);
            Assert.AreEqual("'Last Name' should not be empty.", outcome["Players[0].LastName"]);
            Assert.AreEqual("'DOB' must not be empty.", outcome["Players[0].DOB"]);
            Assert.AreEqual("'Last Name' should not be empty.", outcome["Players[2].LastName"]);
            Assert.AreEqual("", outcome["Players[1].LastName"]);
            var coach = outcome.GetOutcome("Coach");
            Assert.IsFalse(coach.IsValid);
            Assert.AreSame(coach, team.Coach.ValidationOutcome);
            Assert.AreEqual("'First Name' should not be empty.", coach["FirstName"]);
            Assert.AreEqual("'Last Name' should not be empty.", coach["LastName"]);
            var players = outcome.GetOutcome("Players");
            Assert.IsFalse(players.IsValid);
            var player0 = players.GetOutcome("0");
            Assert.IsFalse(player0.IsValid);
            Assert.AreSame(player0, team.Players[0].ValidationOutcome);
            Assert.AreEqual("'First Name' should not be empty.", player0["FirstName"]);
            Assert.AreEqual("'Last Name' should not be empty.", player0["LastName"]);
            Assert.AreEqual("'DOB' must not be empty.", player0["DOB"]);
            Assert.IsNull(players.GetOutcome("1"));
            var player2 = players.GetOutcome("2");
            Assert.IsFalse(player2.IsValid);
            Assert.AreSame(player2, team.Players[2].ValidationOutcome);
            Assert.AreEqual("", player2["FirstName"]);
            Assert.AreEqual("'Last Name' should not be empty.", player2["LastName"]);
            Assert.AreEqual("", player2["DOB"]);
        }
    }
}
