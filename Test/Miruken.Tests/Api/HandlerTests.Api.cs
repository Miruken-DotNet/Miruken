namespace Miruken.Tests.Api
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Api;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Concurrency;

    [TestClass]
    public class HandlerApiTests
    {
        private IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<TeamHandler>();
            factory.RegisterDescriptor<FilterProvider>();
            factory.RegisterDescriptor<Stash>();
            HandlerDescriptorFactory.UseFactory(factory);

            _handler = new TeamHandler() + new FilterProvider();
        }

        [TestMethod]
        public async Task Should_Send_Request_With_Response()
        {
            var team = await _handler.Send(new CreateTeam
            {
                Team = new Team
                {
                    Name = "Liverpool Owen"
                }
            });
            Assert.AreEqual(1, team.Id);
            Assert.IsTrue(team.Active);
        }

        [TestMethod]
        public async Task Should_Send_Request_With_Response_Dynamic()
        {
            var team = await _handler.Send<Team>((object)new CreateTeam
            {
                Team = new Team
                {
                    Name = "Liverpool Owen"
                }
            });
            Assert.AreEqual(1, team.Id);
            Assert.IsTrue(team.Active);
        }

        [TestMethod]
        public async Task Should_Send_Request_Without_Response()
        {
            var team = new Team
            {
                Id = 1,
                Name = "Liverpool Owen",
                Active = true
            };

            await _handler.Send(new RemoveTeam { Team = team });
            Assert.IsFalse(team.Active);
        }

        [TestMethod]
        public async Task Should_Publish_Notifications()
        {
            var teams = new TeamHandler();
            var handler = teams + new FilterProvider();
            var team = await handler.Send(new CreateTeam
            {
                Team = new Team
                {
                    Name = "Liverpool Owen"
                }
            });
            var notifications = teams.Notifications;
            Assert.AreEqual(1, notifications.Count);
            var teamCreated = notifications.First() as TeamCreated;
            Assert.IsNotNull(teamCreated);
            Assert.AreEqual(team.Id, teamCreated.Team.Id);
        }
        public class Team
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Active { get; set; }
            public string Division { get; set; }
        }

        public class TeamAction
        {
            public Team Team { get; set; }
        }

        public class CreateTeam : TeamAction, IRequest<Team>
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

        [Filter(typeof(MetricsFilter<,>))]
        public class TeamHandler : Handler
        {
            public int _teamId;
            private readonly List<object> _notifications = new();

            public ICollection<object> Notifications => _notifications;

            [Handles]
            public Promise<Team> Create(CreateTeam create, IHandler composer)
            {
                var team = create.Team;
                team.Id = ++_teamId;
                team.Active = true;

                composer.Publish(new TeamCreated { Team = team });
                return Promise.Resolved(team);
            }

            [Handles]
            public void Remove(RemoveTeam remove, Command command, IHandler composer)
            {
                var team = remove.Team;
                team.Active = false;
                composer.Publish(new TeamRemoved { Team = team });
            }

            [Handles]
            public void Notify(TeamCreated teamCreated)
            {
                _notifications.Add(teamCreated);
            }

            [Handles]
            public void Notify(TeamRemoved teamRemoved)
            {
                _notifications.Add(teamRemoved);
            }
        }

        public class MetricsFilter<TReq, TResp> : DynamicFilter<TReq, TResp>
        {
            public Task<TResp> Next(TReq request, Next<TResp> next, IHandler composer)
            {
                composer.StashPut("Hello");
                return next();
            }
        }

        private class FilterProvider : Handler
        {
            [Provides]
            public MetricsFilter<TReq, TResp>[] GetMetricsFilter<TReq, TResp>()
            {
                return new[]
                {
                    new MetricsFilter<TReq, TResp>()
                };
            }
        }
    }
}
