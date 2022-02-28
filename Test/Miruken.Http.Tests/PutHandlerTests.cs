namespace Miruken.Http.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Put;

[TestClass]
public class PutHandlerTests : HttpTestScenario
{

    [TestMethod]
    public async Task Should_Perform_Rest_Put()
    {
        var player = await Handler
            .HttpPut<Player, Player>(new Player
            {
                Id   = 2,
                Name = "Sean"
            }, "http://localhost:9000/player");
        Assert.AreEqual(2, player.Id);
        Assert.AreEqual("Sean", player.Name);
    }

    [TestMethod]
    public void Should_Compare_Put_For_Equality()
    {
        var put1 = new PutRequest<Player, Player>();
        var put2 = new PutRequest<Player, Player>
        {
            BaseAddress = "http://localhost:2000"
        };
        var put3 = new PutRequest<Player, Player>(new Player
        {
            Id   = 1,
            Name = "Robert Lewandowski"
        });
        var put4 = new PutRequest<Player, Player>(new Player
        {
            Id   = 4,
            Name = "Gareth Bale"
        });
        var put5 = new PutRequest<Player, Player>(new Player
        {
            Id   = 1,
            Name = "Robert Lewandowski"
        });
        Assert.AreNotEqual(put1, put2);
        Assert.AreNotEqual(put1, put3);
        Assert.AreNotEqual(put3, put4);
        Assert.AreEqual(put3, put5);
    }
}