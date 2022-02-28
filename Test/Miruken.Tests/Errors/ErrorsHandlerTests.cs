namespace Miruken.Tests.Errors;

using System;
using System.Threading.Tasks;
using Error;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Callback;
using Miruken.Concurrency;
using static Protocol;

[TestClass]
public class ErrorsHandlerTests
{
    [TestMethod]
    public void Should_Suppress_Result_By_Default()
    {
        var handled = false;
        var handler = new ErrorsHandler();
        Proxy<IErrors>(handler).HandleException(new Exception("This is bad"))
            .Then((r,s) => handled = true, (ex,s) => handled = true);
        Assert.IsFalse(handled);
    }

    [TestMethod,
     ExpectedException(typeof(OperationCanceledException), AllowDerivedTypes = true)]
    public async Task Should_Throw_Cancalled_When_Awaited()
    {
        var handler = new ErrorsHandler();
        await Proxy<IErrors>(handler).HandleException(new Exception("This is bad"));
    }

    [TestMethod]
    public void Should_Recover_From_Exceptions()
    {
        var handler = new Paymentech() + new ErrorsHandler();
        Proxy<IPayments>(handler.Recover()).ValidateCard("1234");
    }

    [TestMethod]
    public void Should_Recover_From_Exceptions_Async()
    {
        var handled   = false;
        var completed = false;
        var handler   = new Paymentech() + new ErrorsHandler();
        Proxy<IPayments>(handler.Recover()).ProcessPayments(1000M)
            .Then((r, s) => handled = true, (ex, s) => handled = true)
            .Finally(() => completed = true);
        Assert.IsFalse(handled);
        Assert.IsTrue(completed);
    }

    [TestMethod]
    public void Should_Customize_Exceptions_Async()
    {
        var handled  = false;
        var handler  = new CustomErrorHandler() + new Paymentech()
                                                + new ErrorsHandler();
        Proxy<IPayments>(handler.Recover()).ProcessPayments(1000M)
            .Then((r, s) =>
            {
                Assert.AreEqual(Guid.Empty, r);
                handled = true;
            });
        Assert.IsTrue(handled);
    }

    private interface IPayments
    {
        bool ValidateCard(string card);
        Promise<Guid> ProcessPayments(decimal payment);
    }

    private class Paymentech : Handler, IPayments
    {
        bool IPayments.ValidateCard(string card)
        {
            if (card.Length < 10)
                throw new ArgumentException("Card number must have at least 10 digits");
            return true;
        }

        Promise<Guid> IPayments.ProcessPayments(decimal payment)
        {
            if (payment > 500M)
                return Promise<Guid>.Rejected(new ArgumentException(
                    "Amount exceeded limit"));
            return Promise.Resolved(Guid.NewGuid());
        }
    }

    private class CustomErrorHandler : Handler, IErrors
    {
        public Promise HandleException(
            Exception exception, object callback, object context)
        {
            return Promise.Resolved(Guid.Empty);
        }
    }
}