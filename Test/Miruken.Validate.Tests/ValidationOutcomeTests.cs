namespace Miruken.Validate.Tests;

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ValidationOutcomeTests
{
    [TestMethod]
    public void Should_Add_Simple_Error()
    {
        var outcome = new ValidationOutcome();
        outcome.AddError("Name", "Name can't be empty");
        Assert.AreEqual("Name can't be empty", outcome["Name"]);
        Assert.AreEqual("Name can't be empty", outcome.Error);
        CollectionAssert.AreEqual(new[] {"Name"}, outcome.Culprits);
        CollectionAssert.Contains(
            outcome.GetErrors("Name").Cast<object>().ToArray(),
            "Name can't be empty"
        );
    }

    [TestMethod]
    public void Should_Add_Nested_Error()
    {
        var outcome = new ValidationOutcome();
        outcome.AddError("Company.Name", "Name can't be empty");
        Assert.AreEqual($"[{Environment.NewLine}Name can't be empty{Environment.NewLine}]",
            outcome["Company"]);
        CollectionAssert.AreEqual(new[] {"Company"}, outcome.Culprits);
        var company = outcome.GetOutcome("Company");
        Assert.IsFalse(company.IsValid);
        Assert.AreEqual("Name can't be empty", company["Name"]);
        CollectionAssert.AreEqual(new[] {"Name"}, company.Culprits);
        CollectionAssert.Contains(
            outcome.GetErrors("Company").Cast<object>().ToArray(),
            company);
    }

    [TestMethod]
    public void Should_Notify_Simple_Error_Changes()
    {
        string propertyName = null;
        var outcome = new ValidationOutcome();
        outcome.ErrorsChanged += (s, e) => propertyName = e.PropertyName;
        outcome.AddError("Name", "Name can't be empty");
        Assert.AreEqual("Name", propertyName);
    }

    [TestMethod]
    public void Should_Notify_Nested_Error_Changes()
    {
        string propertyName = null;
        var outcome = new ValidationOutcome();
        outcome.ErrorsChanged += (s, e) => propertyName = e.PropertyName;
        outcome.AddError("Company.Name", "Name can't be empty");
        Assert.AreEqual("Company.Name", propertyName);
    }
}