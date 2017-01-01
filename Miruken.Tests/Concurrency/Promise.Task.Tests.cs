using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Concurrency;
using Miruken.Infrastructure;

namespace Miruken.Tests.Concurrency
{
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Summary description for PromiseTaskTests
    /// </summary>
    [TestClass]
    public class PromiseTaskTests
    {
        [TestMethod]
        public void Should_Convert_Pending_Promise_To_Task()
        {
            var promise = new Promise<int>((resolve, reject) => {});
            var task    = promise.ToTask();
            Assert.AreEqual(TaskStatus.WaitingForActivation, task.Status);
        }

        [TestMethod]
        public void Should_Convert_Fulfilled_Promise_To_Task()
        {
            var promise = Promise.Resolved("Hello");
            var task    = promise.ToTask();
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            Assert.AreEqual("Hello", task.Result);
        }

        [TestMethod]
        public void Should_Convert_Rejected_Promise_To_Task()
        {
            var exception = new InvalidOperationException("Can't do that");
            var promise   = Promise.Rejected(exception);
            var task      = promise.ToTask();
            Assert.AreEqual(TaskStatus.Faulted, task.Status);
            Assert.IsNotNull(task.Exception);
            Assert.AreSame(exception, task.Exception.InnerExceptions.First());
        }

        [TestMethod]
        public void Should_Convert_Cancelled_Promise_To_Task()
        {
            var promise   = new Promise<int>((resolve, reject) => {});
            var task      = promise.ToTask();
            promise.Cancel();
            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        [TestMethod]
        public void Should_Fulfill_Asynchronously_Using_Tasks()
        {
            TestRunner.MTA(() =>
            {
                var called   = 0;
                var promise  = new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)));
                var task     = promise.ToTask();
                var fulfill1 = task.ContinueWith(t =>
                {
                    Assert.AreEqual("Hello", task.Result);
                    ++called;
                });
                var fulfill2 = task.ContinueWith(t =>
                {
                    Assert.AreEqual("Hello", t.Result);
                    ++called;
                });
                if (Task.WaitAll(new[] { fulfill1, fulfill2 }, 5.Sec()))
                {
                    Assert.AreEqual(2, called);
                }
                else
                    Assert.Fail("Operation timed out");
            });
        }

        [TestMethod]
        public void Should_Reject_Asynchronously_Using_Tasks()
        {
            TestRunner.MTA(() =>
            {
                var called  = 0;
                var promise = new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ =>
                        reject(new Exception("Rejected"), false)));
                var task    = promise.ToTask();
                var catch1  = task.ContinueWith(t =>
                {
                    Assert.AreEqual("Rejected", 
                        t.Exception?.InnerExceptions.First().Message);
                    ++called;
                });
                var catch2  = task.ContinueWith(t =>
                {
                    Assert.AreEqual("Rejected",
                        t.Exception?.InnerExceptions.First().Message);
                    ++called;
                });
                if (Task.WaitAll(new[] { catch1, catch2 }, 5.Sec()))
                {
                    Assert.AreEqual(2, called);
                }
                else
                    Assert.Fail("Operation timed out");
            });
        }

        [TestMethod]
        public async Task Should_Await_Fulfilled_Promise()
        {
            var promise = Promise.Resolved("Hello");
            Assert.AreEqual("Hello", await promise);
        }

        [TestMethod, ExpectedException(typeof(ArgumentException),
            "Bad parameter")]
        public async Task Should_Await_Rejetced_Promise()
        {
            var exception = new ArgumentException("Bad parameter");
            await Promise.Rejected(exception);
        }

        [TestMethod]
        public void Should_Await_Completed_Promises()
        {
            TestRunner.MTA(async () =>
            {
                var promise = new Promise<object>((resolve, reject) =>
                   ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)));
                Assert.AreEqual("Hello", await promise);
                Assert.AreEqual("Hello", await promise);
            });
        }

        [TestMethod]
        public void Should_Await_Failed_Promises()
        {
            TestRunner.MTA(async () =>
            {
                var promise = new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ =>
                        reject(new Exception("This is bad"), false)));
                try
                {
                    await promise;
                }
                catch (Exception ex)
                {
                    Assert.AreEqual("This is bad", ex.Message);
                }
            });
        }

        [TestMethod]
        public void Should_Convert_Competed_Task_To_Promise()
        {
            var task    = Task.FromResult("Hello");
            var promise = task.ToPromise();
            Assert.AreEqual(PromiseState.Fulfilled, promise.State);
            Assert.AreEqual("Hello", promise.End());
        }

        [TestMethod, ExpectedException(typeof(NotSupportedException),
            "No Scanner Found")]
        public void Should_Convert_Failed_Task_To_Promise()
        {
            var exception = new NotSupportedException("No Scanner Found");
            var task      = Task.FromException<string>(exception);
            var promise   = task.ToPromise();
            Assert.AreEqual(PromiseState.Rejected, promise.State);
            promise.End();
        }

        [TestMethod]
        public void Should_Complete_Tasks_As_Promises()
        {
            TestRunner.MTA(() =>
            {
                var called   = 0;
                var task     = Task.Run(() => "Hello");
                var promise  = task.ToPromise();
                var fulfill1 = promise.Then((r, s) =>
                {
                    Assert.AreEqual("Hello", r);
                    ++called;
                });
                var fulfill2 = promise.Then((r, s) =>
                {
                    Assert.AreEqual("Hello", r);
                    ++called;
                });
                if (WaitHandle.WaitAll(
                    new[] {fulfill1.AsyncWaitHandle, fulfill2.AsyncWaitHandle}, 5.Sec()))
                {
                    Assert.AreEqual(2, called);
                }
                else
                    Assert.Fail("Operation timed out");
            });
        }

        [TestMethod]
        public void Should_Fault_Tasks_As_Promises()
        {
            TestRunner.MTA(() =>
            {
                var called  = 0;
                var task    = Task.Run(() => { throw new Exception("Rejected"); });
                var promise = task.ToPromise();
                var catch1  = promise.Catch((ex, s) =>
                {
                    Assert.AreEqual("Rejected", ex.Message);
                    ++called;
                });
                var catch2  = promise.Catch((ex, s) =>
                {
                    Assert.AreEqual("Rejected", ex.Message);
                    ++called;
                });
                if (WaitHandle.WaitAll(
                    new[] { catch1.AsyncWaitHandle, catch2.AsyncWaitHandle }, 5.Sec()))
                {
                    Assert.AreEqual(2, called);
                }
                else
                    Assert.Fail("Operation timed out");
            });
        }
    }
}
