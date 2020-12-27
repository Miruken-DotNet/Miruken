namespace Miruken.Http.Tests
{
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
            var tryValue = new Try<Exception, string>((string)null);
            var json     = JsonConvert.SerializeObject(tryValue, _settings);
            Assert.AreEqual("{\"isLeft\":false,\"value\":null}", json);
            tryValue = JsonConvert.DeserializeObject<Try<Exception, string>>(json, _settings);
            Assert.IsNotNull(tryValue);
            Assert.IsFalse(tryValue.IsError);
        }

        [TestMethod]
        public void Should_Handle_Null_Right_Try()
        {
            var tryValue = new Try<Exception, string>((Exception)null);
            var json = JsonConvert.SerializeObject(tryValue, _settings);
            Assert.AreEqual("{\"isLeft\":true,\"value\":null}", json);
            tryValue = JsonConvert.DeserializeObject<Try<Exception, string>>(json, _settings);
            Assert.IsNotNull(tryValue);
            Assert.IsTrue(tryValue.IsError);
        }

        [TestMethod]
        public void Should_Handle_Scheduled_Result()
        {
            var response = new ScheduledResult
            {
                Responses = new[]
                {
                    new Try<Exception, object>("Hello"),
                    new Try<Exception, object>(new ScheduledResult()) 
                }
            };
            var json = JsonConvert.SerializeObject(response, _settings);
            Assert.AreEqual("{\"Responses\":[{\"isLeft\":false,\"value\":\"Hello\"},{\"isLeft\":false,\"value\":{}}]}", json);
            response = JsonConvert.DeserializeObject<ScheduledResult>(json, _settings);
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Responses.Length);
        }
    }
}