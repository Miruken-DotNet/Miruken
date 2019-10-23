namespace Miruken.Http.Tests
{
    using Api;
    using Functional;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NETSTANDARD
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
#else
    using System.Net;
    using System.Web.Http;
#endif

    public class Player : IRequest<Player>
    {
        public int    Id   { get; set; }
        public string Name { get; set; }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return other is Player otherPlayer
                   && Id == otherPlayer.Id
                   && Equals(Name, otherPlayer.Name);
        }

        public override int GetHashCode()
        {
            return (Name?.GetHashCode() ?? 0) * 31 + Id;
        }
    }

    public class Team
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
        public Either<Notification, Notification[]> Notifications { get; set; }
    }

    public class Notification
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }

#if NETSTANDARD
    [ApiController, Route("player")]
#else
    [RoutePrefix("player")]
#endif
    public class PlayersController :
#if NETSTANDARD
        ControllerBase
#else
        ApiController
#endif
    {
        [HttpGet, Route("{id}")]
        public Player GetPlayer(int id)
        {
            return new Player { Id = id, Name = "Ronaldo" };
        }

        [HttpPost, Route("")]
        public Player CreatePlayer(Player player)
        {
            player.Id = 1;
            return player;
        }

        [HttpPut, Route("")]
        public Player UpdatePlayer(Player player)
        {
            return player;
        }

        [HttpPatch, Route("")]
        public Player PatchPlayer(Player player)
        {
            return player;
        }

        [HttpDelete, Route("{id}")]
        public void DeletePlayer(int id)
        {
        }
    }

#if NETSTANDARD
    [ApiController, Route("either")]
#else
    [RoutePrefix("either")]
#endif
    public class EitherController :
#if NETSTANDARD
        ControllerBase
#else
        ApiController
#endif
    {
#if NETSTANDARD
        [HttpGet, Route("player/{id}")]
        public IActionResult GetPlayer(int id)
        {
            return id <= 10
                ? Ok(new Player { Id = id, Name = "Messi" })
                : Ok($"unknown id {id}");
        }
#else
        [HttpGet, Route("player/{id}")]
        public IHttpActionResult GetPlayer(int id)
        {
            return id <= 10
                 ? Ok(new Player {Id = id, Name = "Messi"})
                 : (IHttpActionResult) Ok($"unknown id {id}");
        }
#endif
        [HttpGet, Route("team/{id}")]
        public Team GetTeam(int id)
        {
            return id < 10
                 ? new Team
                 {
                     Id = id,
                     Name = "Real Madrid",
                     Notifications = new Notification
                     {
                         Sender = "Zinedine Zidane",
                         Message = "Practice at 6am"
                     }
                 }
                 : new Team
                 {
                     Id = id,
                     Name = "Manchester United",
                     Notifications = new[]
                     {
                             new Notification
                             {
                                 Sender  = "José Mourinho",
                                 Message = "Wayne Rooney was traded"
                             }
                     }
                 };
        }
    }

#if NETSTANDARD
    [ApiController, Route("try")]
#else
    [RoutePrefix("try")]
#endif
    public class TryController :
#if NETSTANDARD
        ControllerBase
#else
        ApiController
#endif
    {
#if NETSTANDARD
        [HttpGet, Route("player/{id}")]
        public IActionResult GetPlayer(int id)
        {
            return id <= 10
                 ? Ok(new Player { Id = id, Name = "Lukakoo" })
                 : StatusCode(StatusCodes.Status400BadRequest, $"bad id {id}");
        }
#else
        [HttpGet, Route("player/{id}")]
        public IHttpActionResult GetPlayer(int id)
        {
            return id <= 10
                 ? Ok(new Player { Id = id, Name = "Lukakoo" })
                 : (IHttpActionResult)Content(
                       HttpStatusCode.BadRequest,
                       $"bad id {id}");
        }
#endif
    }

#if NETSTANDARD
    [ApiController, Authorize, Route("secure_player")]
#else
    [Authorize, RoutePrefix("secplayer")]
#endif
    public class PlayersSecureController :
#if NETSTANDARD
        ControllerBase
#else
        ApiController
#endif
    {
        [HttpGet, Route("{id}")]
        public Player GetPlayer(int id)
        {
            Assert.IsTrue(User.Identity.IsAuthenticated);
            return new Player { Id = id, Name = "Messi" };
        }

        [HttpPost, Route("")]
        public Player CreatePlayer(Player player)
        {
            player.Id = 1;
            return player;
        }

        [HttpPut, Route("")]
        public Player UpdatePlayer(Player player)
        {
            return player;
        }

        [HttpPatch, Route("")]
        public Player PatchPlayer(Player player)
        {
            return player;
        }

        [HttpDelete, Route("{id}")]
        public void DeletePlayer(int id)
        {
        }
    }
}
