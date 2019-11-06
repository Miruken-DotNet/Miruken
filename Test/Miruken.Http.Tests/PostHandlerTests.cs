namespace Miruken.Http.Tests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Api;
    using Api.Route;
    using Post;

    [TestClass]
    public class PostHandlerTests : HttpTestScenario
    {
        [TestMethod]
        public async Task Should_Perform_Http_Post()
        {
            var player = await _handler
                .HttpPost<Player, Player>(new Player
                {
                    Name = "Craig"
                }, "http://localhost:9000/player");
            Assert.AreEqual(1, player.Id);
            Assert.AreEqual("Craig", player.Name);
        }

        [TestMethod,
         ExpectedException(typeof(HttpRequestException))]
        public async Task Should_Propagate_Connection_Errors()
        {
            var post = new PostRequest<Player, Player>(new Player
            {
                Name = "Craig"
            }).RouteTo("http://localhost:9000/coach");
            await _handler.Send(post);
        }

        [TestMethod]
        public void Should_Compare_Post_For_Equality()
        {
            var post1 = new PostRequest<Player, Player>();
            var post2 = new PostRequest<Player, Player>
            {
                BaseAddress = "http://localhost:2000"
            };
            var post3 = new PostRequest<Player, Player>(new Player
            {
                Id   = 1,
                Name = "Robert Lewandowski"
            });
            var post4 = new PostRequest<Player, Player>(new Player
            {
                Id   = 4,
                Name = "Gareth Bale"
            });
            var post5 = new PostRequest<Player, Player>(new Player
            {
                Id   = 1,
                Name = "Robert Lewandowski"
            });
            Assert.AreNotEqual(post1, post2);
            Assert.AreNotEqual(post1, post3);
            Assert.AreNotEqual(post3, post4);
            Assert.AreEqual(post3, post5);
        }
    }
}
