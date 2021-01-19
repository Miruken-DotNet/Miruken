namespace Miruken.Http.Tests
{
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Delete;

    [TestClass]
    public class DeleteHandlerTests : HttpTestScenario
    {
        [TestMethod]
        public async Task Should_Perform_Http_Delete()
        {
            await Handler.HttpDelete<string>(
                "http://localhost:9000/player/2");
        }

        [TestMethod]
        public void Should_Compare_Delete_For_Equality()
        {
            var delete1 = new DeleteRequest<Player, Player>();
            var delete2 = new DeleteRequest<Player, Player>
            {
                BaseAddress = "http://localhost:2000"
            };
            var delete3 = new DeleteRequest<Player, Player>(new Player
            {
                Id   = 1,
                Name = "Robert Lewandowski"
            });
            var delete4 = new DeleteRequest<Player, Player>(new Player
            {
                Id   = 4,
                Name = "Gareth Bale"
            });
            var delete5 = new DeleteRequest<Player, Player>(new Player
            {
                Id   = 1,
                Name = "Robert Lewandowski"
            });
            Assert.AreNotEqual(delete1, delete2);
            Assert.AreNotEqual(delete1, delete3);
            Assert.AreNotEqual(delete3, delete4);
            Assert.AreEqual(delete3, delete5);
        }
    }
}
