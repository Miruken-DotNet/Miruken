namespace Miruken.Http.Tests;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Api;
using Api.Cache;
using Authorize;
using Callback;
using Callback.Policy;
using Functional;
using Get;
using Newtonsoft.Json;

[TestClass]
public class GetHandlerTests : HttpTestScenario
{
    [TestMethod]
    public async Task Should_Perform_Http_Get()
    {
        var player = await Handler.HttpGet<Player>("player/1");
        Assert.AreEqual(1, player.Id);
        Assert.AreEqual("Ronaldo", player.Name);
    }

    [TestMethod]
    public async Task Should_Return_Ownership_Of_HttpResponse()
    {
        var response = await Handler.HttpGet<HttpResponseMessage>("/player/1");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using (response)
        {
            var player = await response.Content.ReadAsAsync<Player>();
            Assert.AreEqual("Ronaldo", player.Name);
        }
    }

    private class TestPipeline
    {
        public Task<HttpResponseMessage> Pipeline(
            HttpRequestMessage request, CancellationToken cancellation,
            IHandler composer, Func<Task<HttpResponseMessage>> next)
        {
            Called = true;
            return next();
        }

        public bool Called { get; private set; }
    }

    [TestMethod]
    public async Task Should_Perform_Http_Get_With_Pipeline()
    {
        var pipeline = new TestPipeline();
        var player   = await Handler
            .Pipeline(pipeline.Pipeline).HttpGet<Player>(
                "http://localhost:9000/player/1");
        Assert.AreEqual(1, player.Id);
        Assert.AreEqual("Ronaldo", player.Name);
        Assert.IsTrue(pipeline.Called);
    }

    [TestMethod]
    public async Task Should_Support_Either_Properties()
    {
        var team = await Handler
            .Formatters(HttpFormatters.Route)
            .HttpGet<Team>("/either/team/2");
        Assert.AreEqual(2, team.Id);
        Assert.AreEqual("Real Madrid", team.Name);
        team.Notifications.Match(
            single =>
            {
                Assert.AreEqual("Zinedine Zidane", single.Sender);
                Assert.AreEqual("Practice at 6am", single.Message);
            },
            multiple => Assert.Fail("Should not get here"));

        team = await Handler
            .Formatters(HttpFormatters.Route)
            .HttpGet<Team>("/either/team/22");
        Assert.AreEqual(22, team.Id);
        Assert.AreEqual("Manchester United", team.Name);
        team.Notifications.Match(
            single => Assert.Fail("Should not get here"),
            multiple =>
            {
                Assert.AreEqual("José Mourinho", multiple[0].Sender);
                Assert.AreEqual("Wayne Rooney was traded", multiple[0].Message);
            });
    }

    [TestMethod]
    public async Task Should_Support_Either()
    {
        var either = await Handler
            .HttpGet<Either<Player, string>>(
                "/either/player/5");
        either.Match(
            player => Assert.AreEqual("Messi", player.Name),
            _ => Assert.Fail("Should not get here"));

        either = await Handler
            .HttpGet<Either<Player, string>>(
                "/either/player/15");
        either.Match(
            _ => Assert.Fail("Should not get here"),
            msg => Assert.AreEqual("unknown id 15", msg.Trim('"')));
    }

    [TestMethod]
    public async Task Should_Support_Either_Of_HttpResponse()
    {
        var either = await Handler
            .HttpGet<Either<HttpResponseMessage, string>>(
                "/either/player/5");
        either.Match(async response =>
            {
                using (response)
                {
                    var player = await response.Content.ReadAsAsync<Player>();
                    Assert.AreEqual("Messi", player.Name);
                }
            },
            _ => Assert.Fail("Should not get here"));

        either = await Handler
            .HttpGet<Either<HttpResponseMessage, string>>(
                "/either/player/15");
        either.Match(async response =>
            {
                using (response)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual("unknown id 15", msg.Trim('"'));
                }
            },
            msg => Assert.Fail("Should not get here"));
    }

    [TestMethod]
    public async Task Should_Support_Nested_Either()
    {
        var either = await Handler
            .HttpGet<Either<Either<int, Player>, string>>(
                "/either/player/5");
        either.Match(
            x => x.Match(
                _ => Assert.Fail("Should not be an int"),
                player => Assert.AreEqual("Messi", player.Name)),
            _ => Assert.Fail("Should not get here"));

        either = await Handler
            .HttpGet<Either<Either<int, Player>, string>>(
                "/either/player/15");
        either.Match(
            _ => Assert.Fail("Should not get here"),
            msg => Assert.AreEqual("unknown id 15", msg.Trim('"')));
    }

    [TestMethod,
     ExpectedException(typeof(JsonReaderException))]
    public async Task Should_Fail_Either_Not_Mapped()
    {
        await Handler.HttpGet<Either<Player, int>>( "/either/player/15");
    }

    [TestMethod]
    public async Task Should_Support_Tuple()
    {
        var tuple = await Handler
            .HttpGet<Tuple<Player, Team>>(
                "/player/7");

        Assert.AreEqual(7, tuple.Item1.Id);
        Assert.AreEqual("Ronaldo", tuple.Item1.Name);
        Assert.AreEqual(7, tuple.Item2.Id);
        Assert.AreEqual("Ronaldo", tuple.Item2.Name);
        Assert.IsNull(tuple.Item2.Notifications);
    }

    [TestMethod]
    public async Task Should_Support_Tuple_Of_HttpResponse()
    {
        var tuple = await Handler
            .HttpGet<Tuple<Player, HttpResponseMessage>>(
                "/player/8");

        using (tuple.Item2)
        {
            Assert.AreEqual(8, tuple.Item1.Id);
            Assert.AreEqual("Ronaldo", tuple.Item1.Name);
            Assert.AreEqual(HttpStatusCode.OK, tuple.Item2.StatusCode);
            var player = await tuple.Item2.Content.ReadAsAsync<Player>();
            Assert.AreEqual(8, player.Id);
            Assert.AreEqual("Ronaldo", player.Name);
        }
    }

    [TestMethod]
    public async Task Should_Support_Try()
    {
        var @try = await Handler
            .HttpGet<Try<string, Player>>(
                "/try/player/5");
        @try.Match(
            _ => Assert.Fail("Should not get here"),
            player => Assert.AreEqual("Lukakoo", player.Name));

        @try = await Handler
            .HttpGet<Try<string, Player>>(
                "/try/player/12");
        @try.Match(
            msg => Assert.AreEqual("bad id 12", msg.Trim('"')),
            _ => Assert.Fail("Should not get here"));
    }

    [TestMethod]
    public async Task Should_Support_Try_With_Exception()
    {
        var @try = await Handler
            .HttpGet<Try<Exception, Player>>(
                "/try/player/5");
        @try.Match(
            _ => Assert.Fail("Should not get here"),
            player => Assert.AreEqual("Lukakoo", player.Name));

        @try = await Handler
            .HttpGet<Try<Exception, Player>>(
                "/try/player/12");
        @try.Match(
            ex => Assert.IsInstanceOfType(ex, typeof(UnsupportedMediaTypeException)),
            _ => Assert.Fail("Should not get here"));
    }

    [TestMethod]
    public async Task Should_Support_Try_Of_HttpResponse()
    {
        var @try = await Handler
            .HttpGet<Try<string, HttpResponseMessage>>(
                "/try/player/5");
        @try.Match(
            _ => Assert.Fail("Should not get here"),
            async response =>
            {
                using (response)
                {
                    var player = await response.Content.ReadAsAsync<Player>();
                    Assert.AreEqual("Lukakoo", player.Name);
                }
            });

        @try = await Handler
            .HttpGet<Try<string, HttpResponseMessage>>(
                "/try/player/12");
        @try.Match(
            msg => Assert.AreEqual("bad id 12", msg.Trim('"')),
            _ => Assert.Fail("Should not get here"));
    }

    [TestMethod]
    public async Task Should_Support_Try_Of_Failed_HttpResponse()
    {
        var @try = await Handler
            .HttpGet<Try<HttpResponseMessage, Player>>(
                "/try/player/5");
        @try.Match(
            _ => Assert.Fail("Should not get here"),
            player => Assert.AreEqual("Lukakoo", player.Name));

        @try = await Handler
            .HttpGet<Try<HttpResponseMessage, Player>>(
                "/try/player/12");
        @try.Match(
            async response =>
            {
                using (response)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual("bad id 12", msg.Trim('"'));
                }
            },
            _ => Assert.Fail("Should not get here"));
    }

    [TestMethod]
    public async Task Should_Support_Nested_Try()
    {
        var @try = await Handler
            .HttpGet<Try<string, Try<int, Player>>>(
                "/try/player/5");
        @try.Match(
            _ => Assert.Fail("Should not get here"),
            x => x.Match(_ => Assert.Fail("Should not get int"),
                player => Assert.AreEqual("Lukakoo", player.Name)));

        @try = await Handler
            .HttpGet<Try<string, Try<int, Player>>>(
                "/try/player/12");
        @try.Match(
            msg => Assert.AreEqual("bad id 12", msg.Trim('"')),
            _ => Assert.Fail("Should not get here"));
    }

    [TestMethod,
     ExpectedException(typeof(JsonReaderException))]
    public async Task Should_Fail_Try_Not_Mapped()
    {
        await Handler.HttpGet<Try<int, Player>>("/try/player/12");
    }

    [TestMethod]
    public async Task Should_Cache_Http_Get()
    {
        var response = await Handler.Send(new GetRequest<object, Player>
        {
            ResourceUri = "/player/1"
        }.Cached());
        Assert.AreEqual("Ronaldo", response.Resource.Name);

        var cached = await Handler.Send(new GetRequest<object, Player>()
        {
            ResourceUri = "/player/1"
        }.Cached());
        Assert.AreEqual("Ronaldo", cached.Resource.Name);
    }

    [TestMethod]
    public async Task Should_Override_Get_Handler()
    {
        HandlerDescriptorFactory.Current.RegisterDescriptor<CachedPlayerHandler>();
        var player = await (new CachedPlayerHandler() + Handler)
            .HttpGet<Player>("/player/1");
        Assert.AreEqual("Matthew", player.Name);
    }

    [TestMethod]
    public void Should_Compare_Get_For_Equality()
    {
        var get1 = new GetRequest<Player, Player>();
        var get2 = new GetRequest<Player, Player>
        {
            BaseAddress = "http://localhost:2000"
        };
        var get3 = new GetRequest<Player, Player>(new Player
        {
            Id = 1,
            Name = "Robert Lewandowski"
        });
        var get4 = new GetRequest<Player, Player>(new Player
        {
            Id = 4,
            Name = "Gareth Bale"
        });
        var get5 = new GetRequest<Player, Player>(new Player
        {
            Id = 1,
            Name = "Robert Lewandowski"
        });
        Assert.AreNotEqual(get1, get2);
        Assert.AreNotEqual(get1, get3);
        Assert.AreNotEqual(get3, get4);
        Assert.AreEqual(get3, get5);
    }

    internal class CachedPlayerHandler : Handler
    {
        [Handles]
        public Task<GetResponse<Player>> Get(GetRequest<object, Player> get)
        {
            return Task.FromResult(new GetResponse<Player>(
                new Player {Name = "Matthew"}
            ));
        }
    }
}
    
[TestClass]
public class SecureGetHandlerTests : SecureHttpTestScenario
{
    [TestMethod]
    public async Task Should_Secure_Http_Get()
    {
        var player = await Handler
            .Basic("evalverde", "1234")
            .HttpGet<Player>(
                "/secure_player/1");
        Assert.AreEqual("Messi", player.Name);
    }
}