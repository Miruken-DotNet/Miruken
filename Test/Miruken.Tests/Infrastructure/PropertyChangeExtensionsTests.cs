using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Miruken.Tests.Infrastructure
{
    using System.ComponentModel;
    using Miruken.Infrastructure;

    [TestClass]
    public class PropertyChangeExtensionsTests
    {
        public class Person
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public string FirstName
            {
                get => _firstName;
                set => PropertyChanged.ChangeProperty(ref _firstName, value, this);
            }
            private string _firstName;
        }

        [TestMethod]
        public void Should_Notify_Property_Changed()
        {
            var called = false;
            var person = new Person();
            person.PropertyChanged += (sender, e) =>
            {
                called = true;
                Assert.AreEqual("FirstName", e.PropertyName);
            };
            person.FirstName = "Donald";
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Ignore_Property_Changed_If_Same()
        {
            var called = false;
            var person = new Person { FirstName = "Donald" };
            person.PropertyChanged += (sender, e) =>
            {
                called = true;
            };
            person.FirstName = "Donald";
            Assert.IsFalse(called);
        }
    }
}
