namespace Miruken.Http.Tests
{
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Patch;

    [TestClass]
    public class PatchHandlerTests : HttpTestScenario
    {
        [TestMethod]
        public async Task Should_Perform_Http_Patch()
        {
            var player = await _handler
                .HttpPatch<Player, Player>(new Player
                {
                    Id   = 2,
                    Name = "Sean"
                }, "http://localhost:9000/player");
            Assert.AreEqual(2, player.Id);
            Assert.AreEqual("Sean", player.Name);
        }

        [TestMethod]
        public void Should_Compare_Patch_For_Equality()
        {
            var patch1 = new PatchRequest<Player, Player>();
            var patch2 = new PatchRequest<Player, Player>
            {
                BaseAddress = "http://localhost:2000"
            };
            var patch3 = new PatchRequest<Player, Player>(new Player
            {
                Id   = 1,
                Name = "Robert Lewandowski"
            });
            var patch4 = new PatchRequest<Player, Player>(new Player
            {
                Id   = 4,
                Name = "Gareth Bale"
            });
            var patch5 = new PatchRequest<Player, Player>(new Player
            {
                Id   = 1,
                Name = "Robert Lewandowski"
            });
            Assert.AreNotEqual(patch1, patch2);
            Assert.AreNotEqual(patch1, patch3);
            Assert.AreNotEqual(patch3, patch4);
            Assert.AreEqual(patch3, patch5);
        }
    }
}
