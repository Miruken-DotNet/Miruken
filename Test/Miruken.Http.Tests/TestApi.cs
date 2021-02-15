namespace Miruken.Http.Tests
{
    using Api;
    using Functional;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    
    [ApiController, Route("player")]
    public class PlayersController : ControllerBase
    {
        [HttpGet, Route("{id}")]
        public Player GetPlayer(int id)
        {
            return new() { Id = id, Name = "Ronaldo" };
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
    
    [ApiController, Route("either")]
    public class EitherController : ControllerBase
    {
        [HttpGet, Route("player/{id}")]
        public IActionResult GetPlayer(int id)
        {
            return id <= 10
                ? Ok(new Player { Id = id, Name = "Messi" })
                : Ok($"unknown id {id}");
        }

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
    
    [ApiController, Route("try")]
    public class TryController : ControllerBase
    {
        [HttpGet, Route("player/{id}")]
        public IActionResult GetPlayer(int id)
        {
            return id <= 10
                 ? Ok(new Player { Id = id, Name = "Lukakoo" })
                 : StatusCode(StatusCodes.Status400BadRequest, $"bad id {id}");
        }
    }
    
    [ApiController, Authorize, Route("secure_player")]
    public class PlayersSecureController : ControllerBase
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
        public Player UpdatePlayer(Player player) => player;

        [HttpPatch, Route("")]
        public Player PatchPlayer(Player player) => player;

        [HttpDelete, Route("{id}")]
        public void DeletePlayer(int id)
        {
        }
    }
}
