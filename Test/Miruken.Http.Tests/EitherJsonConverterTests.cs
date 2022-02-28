namespace Miruken.Http.Tests;

using System;
using Api.Schedule;
using Format;
using Functional;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

[TestClass]
public class EitherJsonConverterTests
{
    private JsonSerializerSettings _settings;

    [TestInitialize]
    public void TestInitialize()
    {
        _settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters        = { EitherJsonConverter.Instance }
        };
    }

    [TestMethod]
    public void Should_Handle_Null_Left_Try()
    {
        var tryWrite = new Try<Exception, string>.Success(null);
        var json     = JsonConvert.SerializeObject(tryWrite, _settings);
        Assert.AreEqual("{\"isLeft\":false,\"value\":null}", json);
        var tryRead = JsonConvert.DeserializeObject<Try<Exception, string>>(json, _settings);
        Assert.IsNotNull(tryRead);
        Assert.IsTrue(tryRead is IEither.IRight);
    }

    [TestMethod]
    public void Should_Handle_Null_Right_Try()
    {
        var tryWrite = new Try<Exception, string>.Failure(null);
        var json = JsonConvert.SerializeObject(tryWrite, _settings);
        Assert.AreEqual("{\"isLeft\":true,\"value\":null}", json);
        var tryRead = JsonConvert.DeserializeObject<Try<Exception, string>>(json, _settings);
        Assert.IsNotNull(tryRead);
        Assert.IsTrue(tryRead is IEither.ILeft);
    }

    [TestMethod]
    public void Should_Handle_Scheduled_Result()
    {
        var response = new ScheduledResult
        {
            Responses = new Try<Exception, object>[]
            {
                new Try<Exception, object>.Success("Hello"),
                new Try<Exception, object>.Success(new ScheduledResult()) 
            }
        };
        var json = JsonConvert.SerializeObject(response, _settings);
        Assert.AreEqual("{\"Responses\":[{\"isLeft\":false,\"value\":\"Hello\"},{\"isLeft\":false,\"value\":{}}]}", json);
        response = JsonConvert.DeserializeObject<ScheduledResult>(json, _settings);
        Assert.IsNotNull(response);
        Assert.AreEqual(2, response.Responses.Length);
    }
}