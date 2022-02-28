namespace Miruken.Http.Tests;

using System;
using Callback.Policy;
using Format;
using Functional;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Validate;

[TestClass]
public class ExceptionJsonConverterTests
{
    private JsonSerializerSettings _settings;

    [TestInitialize]
    public void TestInitialize()
    {
        _settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling  = TypeNameHandling.Auto,
            Converters        =
            {
                EitherJsonConverter.Instance,
                new ExceptionJsonConverter(
                    new ValidationMapping() + new ErrorMapping())
            }
        };
            
        var factory = new MutableHandlerDescriptorFactory();
        factory.RegisterDescriptor<ValidationMapping>();
        factory.RegisterDescriptor<ErrorMapping>();
        HandlerDescriptorFactory.UseFactory(factory);
    }

    [TestMethod]
    public void Should_Add_Exception_Type_Information()
    {
        var tryWrite = new Try<Exception, string>.Failure(
            new NotSupportedException("Not handled"));
        var json     = JsonConvert.SerializeObject(tryWrite, _settings);
        Assert.AreEqual("{\"isLeft\":true,\"value\":{\"$type\":\"Miruken.Http.ExceptionData, Miruken.Http\",\"ExceptionType\":\"System.NotSupportedException, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\",\"Message\":\"Not handled\"}}"
            ,json);
        var tryRead = JsonConvert.DeserializeObject<Try<Exception, string>>(json, _settings);
        Assert.IsNotNull(tryRead);
        Assert.IsTrue(tryRead is IEither.ILeft);
    }

    [TestMethod]
    public void Should_Wrap_Exception_Type_In_Message()
    {
        var outcome = new ValidationOutcome();
        outcome.AddError("Name", "Name can't be empty");
        var tryWrite = new Try<Message, string>.Failure(
            new Message(new ValidationException(outcome)));
        var json = JsonConvert.SerializeObject(tryWrite, _settings);
        Assert.AreEqual(
            "{\"isLeft\":true,\"value\":{\"Payload\":{\"$type\":\"Miruken.Validate.ValidationErrors[], Miruken.Validate\",\"$values\":[{\"PropertyName\":\"Name\",\"Errors\":[\"Name can't be empty\"]}]}}}",
            json);
        var readTry = JsonConvert.DeserializeObject<Try<Message, string>>(json, _settings);
        Assert.IsNotNull(readTry);
        readTry.Match(message =>
        {
            Assert.IsInstanceOfType(message.Payload, typeof(ValidationErrors[]));
        }, _ => throw new Exception("Should be failure"));
    }
}