namespace Miruken.Validate.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Callback;
    using Concurrency;
    using FluentValidation;
    using global::FluentValidation;
    using Infrastructure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Model;

    [TestClass]
    public class FluentValidationValidatorTests
    {
        [TestMethod]
        public async Task Should_Validate_Target()
        {
            var handler = new FluentValidationValidator()
                        + new ValidatorProvider();
            var player  = new Player();
            var outcome = await handler.ValidateAsync(player);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, player.ValidationOutcome);
            Assert.AreEqual("'First Name' should not be empty.", outcome["FirstName"]);
            Assert.AreEqual("'Last Name' should not be empty.", outcome["LastName"]);
            Assert.AreEqual("'DOB' must not be empty.", outcome["DOB"]);
        }

        [TestMethod]
        public async Task Should_Compose_Validation()
        {
            var handler = new FluentValidationValidator()
                        + new ValidatorProvider();
            var team    = new Team
            {
                Name    = "Arsenal",
                Coach   = new Coach(),
                Players = new []
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

            var outcome = await handler.ValidateAsync(team);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, team.ValidationOutcome);
            CollectionAssert.AreEquivalent(new [] { "Coach", "Players"}, outcome.Culprits);
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

        [TestMethod]
        public async Task Should_Validation_Target_For_Scope()
        {
            var handler = new FluentValidationValidator()
                        + new ValidatorProvider();
            var team = new Team
            {
                Name    = "Arsenal",
                Coach   = new Coach(),
                Players = new[]
                {
                    new Player
                    {
                        FirstName = "Alexis",
                        LastName = "Sanchez",
                        DOB = new DateTime(1988, 12, 19)
                    },
                    new Player
                    {
                        FirstName = "Wayne",
                        DOB = new DateTime(1985, 10, 24)
                    }
                }
            };

            var outcome = await handler
                .ValidateAsync(team, Scopes.Default, "Quickfoot");
            Assert.IsFalse(outcome.IsValid);
            var errors = outcome.GetErrors("Players").Cast<object>().ToArray();
            CollectionAssert.Contains(errors, "Must have between 3 and 6 players.");
            var players = outcome.GetOutcome("Players");
            Assert.IsFalse(players.IsValid);
            Assert.AreEqual("'Last Name' should not be empty.", players["1.LastName"]);
        }

        [TestMethod]
        public async Task Should_Handle_Cascade_Properly()
        {
            var handler = new FluentValidationValidator()
                        + new FooValidatorProvider()
                        + new FooBarHandler();
            var foo     = new Foo
            {
                Id   = Guid.Empty,
                Name = "z"
            };
            var outcome = await handler.ValidateAsync(foo);
            Console.WriteLine(outcome.Error);
            Assert.IsFalse(outcome.IsValid);
        }

        [TestMethod]
        public async Task Should_Handle_Callback_With_Validators()
        {
            var handler = new FluentValidationValidator()
                        + new FooValidatorProvider()
                        + new FooBarHandler();
            var foo = new Foo
            {
                Id   = Guid.NewGuid(),
                Name = "Spike"
            };
            var bar = await handler.CommandAsync<Bar>(foo);
            Assert.IsNotNull(bar);
        }

        [TestMethod]
        public async Task Should_Detect_Missing_Dependencies()
        {
            var handler = new FluentValidationValidator()
                        + new FooValidatorProvider()
                        + new FooBarHandler { NoBar = true };
            var foo = new Foo
            {
                Id   = Guid.NewGuid(),
                Name = "Patch"
            };
            try
            {
                await handler.CommandAsync<Bar>(foo);
                Assert.Fail("Expected an exception");
            }
            catch (InvalidOperationException)
            {
                // Asynchronous failure
            }
            catch (NotSupportedException)
            {
                // Synchronous failure
            }
        }
    }

    public class Foo
    {
        public Guid?  Id   { get; set; }
        public string Name { get; set; }
    }

    public class Bar
    {
    }

    public class Baz
    {
    }

    public class FooBarHandler : Handler
    {
        public bool NoBar { get; set; }

        [Provides]
        public Bar Bar { get; private set; }

        [Handles, Filter(typeof(ValidateFilter<,>))]
        public Task<Bar> Handle(Foo foo, Bar bar)
        {
            return Task.FromResult(bar);
        }

        [Handles]
        public Promise Handle(Baz baz)
        {
             return Promise.Delay(10.Millis())
                      .Then((res, _) =>
                 {
                     if (!NoBar) Bar = new Bar();
                 });
        }
    }

    public class FooValidator : AbstractValidator<Foo>
    {
        public FooValidator()
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEqual(Guid.Empty);

            RuleFor(x => x)
                .WithComposerCustomAsync(async (foo, ctx, ct, c) =>
                {
                    await c.CommandAsync(new Baz());
                });

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .MustAsync((name, ct) => Task.FromResult(name.Length > 3))
                .CustomAsync((name, ctx, ct) =>
                {
                    if (name.StartsWith("z"))
                        ctx.AddFailure("Name cannot start with z");
                    return Task.CompletedTask;
                });
        }
    }

    public class FooValidatorProvider : Handler
    {
        [Provides]
        public IValidator<Foo>[] GetFooValidators()
        {
            return new[] { new FooValidator() };
        }

        [Provides]
        public ValidateFilter<TCb, TRes> Create<TCb, TRes>()
        {
            return new ValidateFilter<TCb, TRes>();
        }
    }

    public class TeamValidator : AbstractValidator<Team>
    {
        public TeamValidator()
        {
            RuleFor(p => p.Name).NotEmpty();
            RuleFor(p => p.Coach).NotNull().Valid();
            RuleFor(p => p.Division).Matches(@"^[u|U]\d\d?$");
            RuleForEach(p => p.Players).NotEmpty().Valid();

            RuleSet("Quickfoot", () =>
            {
                RuleFor(p => p.Players)
                    .Must(HaveBetweenThreeAndSixPlayers)
                    .WithMessage("Must have between 3 and 6 players.");
            });
        }

        private static bool HaveBetweenThreeAndSixPlayers(Player[] players)
        {
            var playerCount = players.Length;
            return playerCount >= 3 && playerCount <= 6;
        }
    }

    public class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleFor(p => p.FirstName).NotEmpty();
            RuleFor(p => p.LastName).NotEmpty();
        }
    }

    public class CoachValidator : AbstractValidator<Coach>
    {
        public CoachValidator()
        {
            RuleFor(c => c.License).NotEmpty();
        }
    }

    public class PlayerValidator : AbstractValidator<Player>
    {
        public PlayerValidator()
        {
            RuleFor(p => p.DOB).NotNull();
        }
    }

    public class ValidatorProvider : Handler
    {
        [Provides]
        public IValidator<Person>[] GetPersonValidators()
        {
            return new[] { new PersonValidator() };
        }

        [Provides]
        public IValidator<Team>[] GetTeamValidators()
        {
            return new[] { new TeamValidator() };
        }

        [Provides]
        public IValidator<Coach>[] GetCoachValidators()
        {
            return new[] { new CoachValidator() };
        }

        [Provides]
        public IValidator<Player>[] GetPlayerValidators()
        {
            return new[] { new PlayerValidator() };
        }
    }
}
