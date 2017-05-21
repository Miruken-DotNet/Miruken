namespace Miruken.Tests.Mediator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Concurrency;
    using Miruken.Mediator;

    [TestClass]
    public class HandlerMediatorTests
    {
        [TestMethod]
        public async Task Should_Send_Request_With_Response()
        {
            var handler = new TeamHandler() + new PipelineBehaviorProvider();
            var team    = await handler.Send(new CreateTeam
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
        public async Task Should_Send_Request_With_Response_Async()
        {
            var handler = new TeamHandler() + new PipelineBehaviorProvider();
            var team    = await handler.Send(new CreateTeam
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
            var handler = new TeamHandler() + new PipelineBehaviorProvider();
            var team    = new Team
            {
                Id     = 1,
                Name   = "Liverpool Owen",
                Active = true
            };

            await handler.Send(new RemoveTeam { Team = team });
            Assert.IsFalse(team.Active);
        }

        [TestMethod]
        public async Task Should_Publish_Notifiations()
        {
            var teams   = new TeamHandler();
            var handler = teams + new PipelineBehaviorProvider();
            var team    = await handler.Send(new CreateTeam
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
            public int    Id     { get; set; }
            public string Name   { get; set; }
            public bool   Active { get; set; }
        }

        public class CreateTeam : IRequest<Team>
        {
            public Team Team { get; set; }
        }

        public class TeamCreated : INotification
        {
            public Team Team { get; set; }
        }

        public class RemoveTeam : IRequest
        {
            public Team Team { get; set; }
        }

        public class TeamRemoved : INotification
        {
            public Team Team { get; set; }
        }

        public class TeamHandler : Mediator
        {
            public int _teamId;
            private readonly List<INotification> 
                _notifications = new List<INotification>();

            public ICollection<INotification> Notifications => _notifications;

            [Mediates]
            public Promise<Team> Create(CreateTeam create, IHandler composer)
            {
                var team = create.Team;
                team.Id     = ++_teamId;
                team.Active = true;

                composer.Publish(new TeamCreated {Team = team});
                return Promise.Resolved(team);
            }

            [Mediates]
            public void Remove(RemoveTeam remove, IHandler composer)
            {
                var team = remove.Team;
                team.Active = false;
                composer.Publish(new TeamRemoved {Team = team});
            }

            [Mediates]
            public void Notify(INotification notification)
            {
                _notifications.Add(notification);
            }
        }

        private class LogFilter<Cb, Res> : IPipelineBehavior<Cb, Res>
        {
            public int? Order { get; set; }

            public Promise<Res> Filter(Cb callback, MethodBinding method,
                IHandler composer, FilterDelegate<Promise<Res>> next)
            {
                Console.WriteLine($"Handle {callback}");
                return next();
            }
        }

        private class PipelineBehaviorProvider : Handler
        {
            [Provides]
            public IPipelineBehavior<TRequest, TResponse>[] GetBehaviors<TRequest, TResponse>()
            {
                return new[]
                {
                    new LogFilter<TRequest, TResponse>()
                };
            }
        }
    }
}
