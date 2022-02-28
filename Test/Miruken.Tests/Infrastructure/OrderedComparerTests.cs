namespace Miruken.Tests.Infrastructure;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Infrastructure;

[TestClass]
public class OrderedComparerTests
{
    [TestMethod]
    public void Should_Order_Explicitly()
    {
        var message1 = new Message(10);
        var message2 = new Message(2);
        var message3 = new Message(30);

        var messages = new[] { message1, message2, message3 };
        Array.Sort(messages, OrderedComparer<Message>.Instance);

        Assert.AreSame(message2, messages[0]);
        Assert.AreSame(message1, messages[1]);
        Assert.AreSame(message3, messages[2]);
    }

    [TestMethod]
    public void Should_Order_Implicitly()
    {
        var message1 = new Message(50);
        const string message2 = "Hello";
        var message3 = new Message();
        var message4 = new Message(11);

        var messages = new object[] { message1, message2, message3, message4 };
        Array.Sort(messages, OrderedComparer<object>.Instance);

        Assert.AreSame(message4, messages[0]);
        Assert.AreSame(message1, messages[1]);
    }

    public class Message : IOrdered
    {
        public Message(int? order = null)
        {
            Order = order;
        }

        public int? Order { get; set; }
    }
}