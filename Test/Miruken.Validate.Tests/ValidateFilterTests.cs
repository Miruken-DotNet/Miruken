// ReSharper disable UnusedMember.Local
namespace Miruken.Validate.Tests;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Callback;
using Callback.Policy;
using Concurrency;
using DataAnnotations;
using FluentValidation;
using global::FluentValidation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model;
using Validate;

[TestClass]
[SuppressMessage("ReSharper", "CA1822")]
public class ValidateFilterTests
{
    private IHandler _handler;
    private IHandlerDescriptorFactory _factory;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        Handles.Policy.AddFilters(new ValidateAttribute());
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _factory = new MutableHandlerDescriptorFactory();
        _factory.RegisterDescriptor<TeamHandler>();
        _factory.RegisterDescriptor<FilterProvider>();
        _factory.RegisterDescriptor<DataAnnotationsValidator>();
        _factory.RegisterDescriptor<FluentValidationValidator>();
        HandlerDescriptorFactory.UseFactory(_factory);

        _handler = new TeamHandler()
                   + new FilterProvider()
                   + new DataAnnotationsValidator()
                   + new FluentValidationValidator();
    }

    [TestMethod]
    public async Task Should_Validate_Command()
    {
        var team = await _handler.CommandAsync<Team>(new CreateTeam
        {
            Team = new Team
            {
                Name  = "Liverpool Owen",
                Coach = new Coach
                {
                    FirstName = "Zinedine",
                    LastName  = "Zidane",
                    License   = "A"
                }
            }
        });
        Assert.IsTrue(team.ValidationOutcome.IsValid);
        Assert.AreEqual(1, team.Id);
        Assert.IsTrue(team.Active);
    }

    [TestMethod]
    public async Task Should_Match_All_Validators()
    {
        try
        {
            await _handler.CommandAsync(new RemoveTeam
            {
                Team = new Team()
            });
            Assert.Fail("must not succeed");
        }
        catch (Validate.ValidationException vex)
        {
            var outcome = vex.Outcome;
            Assert.IsNotNull(outcome);
            var team = outcome.GetOutcome("Team");
            Assert.IsNotNull(team);
            CollectionAssert.AreEqual(new[] { "Id" }, team.Culprits);
            Assert.AreEqual("'Team Id' must be greater than '0'.", team["Id"]);
        }
    }

    [TestMethod]
    public async Task Should_Reject_Invalid_Command()
    {
        try
        {
            await _handler.CommandAsync(new CreateTeam());
        }
        catch (Validate.ValidationException vex)
        {
            var outcome = vex.Outcome;
            Assert.IsNotNull(outcome);
            CollectionAssert.AreEqual(new[] { "Team" }, outcome.Culprits);
            Assert.AreEqual("'Team' must not be empty.", outcome["Team"]);
        }
    }

    [TestMethod]
    public async Task Should_Reject_Invalid_Callback_Content()
    {
        try
        {
            await _handler.CommandAsync(new CreateTeam
            {
                Team = new Team
                {
                    Coach = new Coach
                    {
                        FirstName = "Zinedine",
                        LastName  = "Zidane",
                        License   = "A"
                    }
                }
            });
            Assert.Fail("must not succeed");
        }
        catch (Validate.ValidationException vex)
        {
            var outcome = vex.Outcome;
            Assert.IsNotNull(outcome);
            var team = outcome.GetOutcome("Team");
            Assert.IsNotNull(team);
            CollectionAssert.AreEqual(new[] { "Name" }, team.Culprits);
            Assert.AreEqual($"The Name field is required.{Environment.NewLine}'Name' must not be empty.", team["Name"]);
        }
    }

    public class TeamAction
    {
        public Team Team { get; set; }
    }

    public class CreateTeam : TeamAction
    {
    }

    public class TeamCreated : TeamAction
    {
    }

    public class RemoveTeam : TeamAction
    {
    }

    public class TeamRemoved : TeamAction
    {
    }

    public class TeamIntegrity : AbstractValidator<Team>
    {
        public TeamIntegrity()
        {
            RuleFor(t => t.Name).NotEmpty();
        }
    }

    public class TeamActionIntegrity : AbstractValidator<TeamAction>
    {
        public TeamActionIntegrity()
        {
            RuleFor(ta => ta.Team).NotEmpty().Valid();
        }
    }

    public class RemoveTeamIntegrity : AbstractValidator<RemoveTeam>
    {
        public RemoveTeamIntegrity()
        {
            RuleFor(ta => ta.Team.Id).GreaterThan(0);
        }
    }

    public class TeamHandler : Handler
    {
        private int _teamId;

        [Handles]
        public Promise<Team> Create(CreateTeam create, IHandler composer)
        {
            var team = create.Team;
            team.Id = ++_teamId;
            team.Active = true;

            composer.CommandAllAsync(new TeamCreated { Team = team });
            return Promise.Resolved(team);
        }

        [Handles]
        public void Remove(RemoveTeam remove, IHandler composer)
        {
            var team = remove.Team;
            team.Active = false;
            composer.CommandAllAsync(new TeamRemoved { Team = team });
        }
    }

    private class FilterProvider : Handler
    {
        [Provides]
        public ValidateFilter<TCb, TRes> Create<TCb, TRes>() => new();

        [Provides]
        public IValidator<Team>[] TeamValidators() =>
            new IValidator<Team>[] { new TeamIntegrity() };

        [Provides]
        public IValidator<TeamAction>[] TeamActionValidators() =>
            new IValidator<TeamAction>[] { new TeamActionIntegrity() };

        [Provides]
        public IValidator<RemoveTeam>[] RemoveTeamValidators() =>
            new IValidator<RemoveTeam>[] { new RemoveTeamIntegrity() };
    }
}