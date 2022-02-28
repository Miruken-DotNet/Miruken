namespace Miruken.Tests.Callback;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Callback;
using Miruken.Callback.Policy;
using Miruken.Callback.Policy.Bindings;

[TestClass]
public class HandlerBindingTests
{
    private IHandler _handler;

    [TestInitialize]
    public void Setup()
    {
        _handler = new StaticHandler();
        var factory = new MutableHandlerDescriptorFactory();
        factory.RegisterDescriptor<PersonProvider>();
        factory.RegisterDescriptor<LocalConfiguration>();
        factory.RegisterDescriptor<RemoteConfiguration>();
        factory.RegisterDescriptor<Hospital>();
        factory.RegisterDescriptor<Client>();
        HandlerDescriptorFactory.UseFactory(factory);
    }

    [TestMethod]
    public void Should_Resolve_Without_Constraints()
    {
        var configuration = _handler.Resolve<IConfiguration>();
        Assert.IsNotNull(configuration);
    }

    [TestMethod]
    public void Should_Resolve_All_Without_Constraints()
    {
        var configurations = _handler.ResolveAll<IConfiguration>();
        Assert.AreEqual(2, configurations.Length);
    }

    [TestMethod]
    public void Should_Resolve_With_Name()
    {
        var configuration = _handler.Resolve<IConfiguration>(
            c => c.Named("local"));
        Assert.IsInstanceOfType(configuration, typeof(LocalConfiguration));

        configuration = _handler.Resolve<IConfiguration>(
            c => c.Named("remote"));
        Assert.IsInstanceOfType(configuration, typeof(RemoteConfiguration));
    }

    [TestMethod]
    public void Should_Resolve_All_With_Name()
    {
        var configurations = _handler.ResolveAll<IConfiguration>(
            c => c.Named("remote"));
        Assert.AreEqual(1, configurations.Length);
        Assert.IsInstanceOfType(configurations[0], typeof(RemoteConfiguration));
    }

    [TestMethod]
    public void Should_Inject_Based_On_Name()
    {
        var client = _handler.Resolve<Client>();
        Assert.IsNotNull(client);
        Assert.IsNotNull(client.Local);
        Assert.AreEqual("http://localhost/Server", client.Local.ServerUrl);
        Assert.IsNotNull(client.Remote);
        Assert.AreEqual("http://remote/Server", client.Remote.ServerUrl);
    }

    [TestMethod]
    public void Should_Resolve_Based_On_Metadata()
    {
        var handler = new PersonProvider();
        var doctor  = handler.Resolve<IPerson>(
            c => c.Require(new DoctorAttribute()));
        Assert.AreEqual("Jack", doctor.FirstName);
        Assert.AreEqual("Zigler", doctor.LastName);

        var programmer = handler.Resolve<IPerson>(
            c => c.Require(new ProgrammerAttribute()));
        Assert.AreEqual("Paul", programmer.FirstName);
        Assert.AreEqual("Allen", programmer.LastName);
    }

    [TestMethod]
    public void Should_Resolve_All_Based_On_Metadata()
    {
        var programmers = new PersonProvider().ResolveAll<IPerson>(
            c => c.Require(new ProgrammerAttribute()));
        Assert.AreEqual(1, programmers.Length);
        Assert.AreEqual("Paul", programmers[0].FirstName);
        Assert.AreEqual("Allen", programmers[0].LastName);
    }

    [TestMethod]
    public void Should_Inject_Based_On_Metadata()
    {
        var handler  = new PersonProvider() + _handler;
        var hospital = handler.Resolve<Hospital>();
        Assert.IsNotNull(hospital);
        Assert.AreEqual("Jack", hospital.Doctor.FirstName);
        Assert.AreEqual("Zigler", hospital.Doctor.LastName);
        Assert.AreEqual("Paul", hospital.Programmer.FirstName);
        Assert.AreEqual("Allen", hospital.Programmer.LastName);
    }

    public interface IPerson
    {
        string FirstName { get; set; }
        string LastName  { get; set; }
    }

    public class Person : IPerson
    {
        public string FirstName { get; set; }
        public string LastName  { get; set; }
    }

    public class DoctorAttribute : ConstraintAttribute
    {
        public DoctorAttribute() : base("Job", "Doctor")
        {             
        }
    }

    public class ProgrammerAttribute : QualifierAttribute
    {
    }

    public class Hospital
    {
        public Hospital(
            [Doctor]     IPerson doctor,
            [Programmer] IPerson programmer)
        {
            Doctor     = doctor;
            Programmer = programmer;
        }

        public IPerson Doctor     { get; }
        public IPerson Programmer { get; }
    }

    public class PersonProvider : Handler
    {
        [Provides, Singleton, Doctor]
        public IPerson GetDoctor()
        {
            return new Person
            {
                FirstName = "Jack",
                LastName  = "Zigler"
            };
        }

        [Provides, Singleton, Programmer]
        public IPerson GetProgrammer()
        {
            return new Person
            {
                FirstName = "Paul",
                LastName  = "Allen"
            };
        }
    }

    public interface IConfiguration
    {
        string ServerUrl { get; }
    }

    public class LocalConfiguration : IConfiguration
    {
        [Singleton, Named("local")]
        public LocalConfiguration()
        {
        }

        public string ServerUrl { get; } = "http://localhost/Server";
    }

    public class RemoteConfiguration : IConfiguration
    {
        [Provides, Singleton, Named("remote")]
        public RemoteConfiguration()
        {            
        }

        public string ServerUrl { get; } = "http://remote/Server";
    }

    public class Client
    {
        public Client(
            [Named("local")]  IConfiguration local,
            [Named("remote")] IConfiguration remote)
        {
            Local  = local;
            Remote = remote;
        }

        public IConfiguration Local  { get; }
        public IConfiguration Remote { get; }
    }
}