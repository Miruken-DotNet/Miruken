namespace Miruken.Tests.Infrastructure;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Infrastructure;

[TestClass]
public class TypedKeyedCollectionTests
{
    [TestMethod]
    public void Should_Add_Item()
    {
        var items = new TypeKeyedCollection<object> {"Hello"};
        CollectionAssert.Contains(items, "Hello");
    }

    [TestMethod,
     ExpectedException(typeof(ArgumentException))]
    public void Should_Not_Add_Item_With_Existing_Type()
    {
        var items = new TypeKeyedCollection<object> { "Hello" };
        CollectionAssert.Contains(items, "Hello");
        items.Add("Goodbye");
    }

    [TestMethod]
    public void Should_Retrieve_Item_By_Type()
    {
        const string item = "Hello";
        var items = new TypeKeyedCollection<object> { item };
        Assert.AreSame(item, items.Find<string>());
    }

    [TestMethod]
    public void Should_Return_Default_If_Not_Found()
    {
        var items = new TypeKeyedCollection<object>();
        Assert.IsNull(items.Find<string>());
        Assert.AreEqual(0, items.Find<int>());
    }

    [TestMethod]
    public void Should_Remove_Item()
    {
        var items = new TypeKeyedCollection<object> { "Hello" };
        Assert.IsTrue(items.Remove("Hello"));
        Assert.IsNull(items.Find<string>());
    }
}