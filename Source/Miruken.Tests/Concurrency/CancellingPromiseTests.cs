using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Concurrency;
using Miruken.Infrastructure;

namespace Miruken.Tests.Concurrency
{
    /// <summary>
    /// Summary description for PromiseTests
    /// </summary>
    [TestClass]
    public class CancellingPromiseTests
    {
        [TestMethod]
        public void Should_Notify_Promise_Owner_When_Cancelled()
        {
            var called  = false;
            var promise = new Promise<object>(
                delegate(Promise<object>.ResolveCallbackT resolve, RejectCallback reject, Action<Action> onCancel)
                {
                    onCancel(() => called = true);
                });
            promise.Cancel();
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Not_Notify_Promise_Owner_Cancelled_When_Fulfilled()
        {
            var called  = false;
            var promise = new Promise<object>((resolve, reject, onCancel) => {
                onCancel(() => called = true);
                resolve("Hello", true);
            });

            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Fulfilled, promise.State);
                Assert.IsFalse(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Not_Notify_Promise_Owner_Cancelled_When_Rejected()
        {
            var called  = false;
            var promise = new Promise<object>((resolve, reject, onCancel) =>
            {
                onCancel(() => called = true);
                reject(new Exception("Bad"), true);
            });

            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Rejected, promise.State);
                Assert.IsFalse(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Notify_Promise_Owner_When_All_Children_Cancelled()
        {
            var called  = false;
            var promise = new Promise<object>((resolve, reject, onCancel) =>
                onCancel(() => called = true));
            var child1 = promise.Then((r, s) => { });
            var child2 = promise.Then((r, s) => { });
            child1.Cancel();
            Assert.AreEqual(PromiseState.Pending, promise.State);
            child2.Cancel();
            Assert.AreEqual(PromiseState.Cancelled, promise.State);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Notify_Promise_Owner_When_Any_Child_Cancelled()
        {
            var called = false;
            var promise = new Promise<object>(
                ChildCancelMode.Any, (resolve, reject, onCancel) =>
                onCancel(() => called = true));
            var child1 = promise.Then((r, s) => { });
            var child2 = promise.Then((r, s) => { });
            child2.Catch((ex, s) => { });
            child1.Cancel();
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Notify_Promise_Owner_When_Nested_Children_Cancelled()
        {
            var called  = false;
            var promise = new Promise<object>((resolve, reject, onCancel) =>
                onCancel(() => called = true));
            var child1 = promise.Then((r, s) => { });
            var child2 = promise.Then((r, s) => { });
            var grandChild1a = child1.Then((r, s) => { });
            var grandChild1b = child1.Then((r, s) => { });
            var grandChild2a = child2.Then((r, s) => { });
            var grandChild2b = child2.Then((r, s) => { });
            grandChild1a.Cancel();
            Assert.AreEqual(PromiseState.Pending, promise.State);
            grandChild1b.Cancel();
            Assert.AreEqual(PromiseState.Pending, promise.State);
            grandChild2a.Cancel();
            Assert.AreEqual(PromiseState.Pending, promise.State);
            grandChild2b.Cancel();
            Assert.AreEqual(PromiseState.Cancelled, promise.State);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Notify_Promise_Owner_When_Any_Nested_Child_Cancelled()
        {
            var called = false;
            var promise = new Promise<object>(
                ChildCancelMode.Any, (resolve, reject, onCancel) =>
                onCancel(() => called = true));
            var child1 = promise.Then((r, s) => { });
            var child2 = promise.Then((r, s) => { });
            var grandChild1a = child1.Then((r, s) => { });
            grandChild1a.Then((r, s) => { });
            var grandChild1b = child1.Then((r, s) => { });
            grandChild1b.Catch((ex, s) => { });
            var grandChild2a = child2.Then((r, s) => { });
            grandChild2a.Finally(() => {});
            var grandChild2b = child2.Then((r, s) => { });
            grandChild2b.Cancel();
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Notify_Promise_Owner_When_Catch_Cancelled()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject, onCancel) =>
                onCancel(() => called = true))
                .Then((r, s) => { })
                .Catch((TimeoutException tex, bool s) => { })
                .Catch((ex, s) => { }); ;
            promise.Cancel();
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Notify_Promise_Owner_When_Finally_Cancelled()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject, onCancel) =>
                onCancel(() => called = true))
                .Finally(() => {});
            promise.Cancel();
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Use_Promise_Owner_Cancellation()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject, onCancel) =>
                onCancel(() => called = true));
            promise.Cancel();
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Use_Promise_Owner_Cancellation_For_Children()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject, onCancel) =>
                onCancel(() => called = true));
            var child1 = promise.Then((r, s) => { });
            var child2 = promise.Then((r, s) => { });
            var grandChild1a = child1.Then((r, s) => { });
            var grandChild1b = child1.Then((r, s) => { });
            var grandChild2a = child2.Then((r, s) => { });
            var grandChild2b = child2.Then((r, s) => { });
            grandChild1a.Cancel();
            Assert.AreEqual(PromiseState.Pending, promise.State);
            grandChild1b.Cancel();
            Assert.AreEqual(PromiseState.Pending, promise.State);
            grandChild2a.Cancel();
            Assert.AreEqual(PromiseState.Pending, promise.State);
            grandChild2b.Cancel();
            Assert.AreEqual(PromiseState.Cancelled, promise.State);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Notify_Unwrapped_Promise_Owner_When_Cancelled()
        {
            var called = false;
            var promise = new Promise<object>(
                ChildCancelMode.Any, (resolve, reject, onCancel) => onCancel(() => called = true))
                .Then((r, s) => new Promise<object>(ChildCancelMode.Any, (res, rej) => { }));
            promise.Cancel();
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }
    }
}
