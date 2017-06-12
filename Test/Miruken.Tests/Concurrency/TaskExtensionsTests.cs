namespace Miruken.Tests.Concurrency
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Concurrency;

    [TestClass]
    public class TaskExtensionsTests
    {
        [TestMethod]
        public void Should_Cast_Fulfilled_Task()
        {
            Task task  = Task.FromResult(1);
            var  taskt = task.Cast<int>();
            Assert.AreEqual(1, taskt.Result);
        }

        [TestMethod]
        public void Should_Cast_Fulfilled_Typed_Task()
        {
            var task  = Task.FromResult(new [] {1});
            var taskt = task.Cast<ICollection<int>>();
            CollectionAssert.AreEquivalent(new [] {1}, taskt.Result.ToArray());
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException))]
        public async Task Should_Cast_Faulted_Task()
        {
            var task  = Task.FromException(new ArgumentException("Blah!"));
            var taskt = task.Cast<int>();
            await taskt;
        }

        [TestMethod,
         ExpectedException(typeof(OperationCanceledException),
            AllowDerivedTypes = true)]
        public async Task Should_Cast_Cancelled_Task()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            var task   = Task.FromCanceled(source.Token);
            var taskt  = task.Cast<int>();
            Assert.AreEqual(1, await taskt);
        }

        [TestMethod,
         ExpectedException(typeof(OperationCanceledException), 
            AllowDerivedTypes = true)]
        public async Task Should_Cast_Cancelled_Type_Task()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            Task task  = Task.FromCanceled<int>(source.Token);
            await task.Cast<int>();
        }

        [TestMethod]
        public void Should_Coerce_Fufilled_Task()
        {
            Task task  = Task.FromResult("ABC");
            var  taskt = (Task<string>)task.Coerce(typeof(Task<string>));
            Assert.AreEqual("ABC", taskt.Result);
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException))]
        public async Task Should_Coerce_Faulted_Task()
        {
            var task  = Task.FromException(new ArgumentException("Blah!"));
            var taskt = task.Coerce(typeof(Task<string>));
            await taskt;
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException))]
        public void Should_Reject_Invalid_Coerce()
        {
            Task task = Task.FromResult("ABC");
            var taskt = (Task<string>)task.Coerce(typeof(Promise<string>));
            Assert.AreEqual("ABC", taskt.Result);
        }
    }
}
