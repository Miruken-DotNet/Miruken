using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Concurrency;
using Miruken.Infrastructure;

namespace Miruken.Tests.Concurrency
{
    /// <summary>
    /// Summary description for PromiseTests
    /// </summary>
    [TestClass]
    public class PromiseTests
    {
        [TestMethod]
        public void Should_Start_Out_In_Pending_State()
        {
            var promise = new Promise<object>(
                (resolve, reject) => {});
            Assert.AreEqual(PromiseState.Pending, promise.State);
        }

        [TestMethod]
        public void Should_Move_To_Fulfilled_State_When_Resolved()
        {
            var promise = new Promise<object>(
                (resolve, reject) => resolve("Hello", true));
            Assert.AreEqual(PromiseState.Fulfilled, promise.State);
        }

        [TestMethod]
        public void Should_Move_To_Rejected_State_When_Rejected()
        {
            var promise = new Promise<object>(
                (resolve, reject) => reject(new Exception("Rejected"), true));
            Assert.AreEqual(PromiseState.Rejected, promise.State);
        }

        [TestMethod]
        public void Should_Fulfill_With_Value()
        {
            var promise = Promise.Resolved(2);
            Assert.AreEqual(2, promise.End());
            Assert.AreEqual(PromiseState.Fulfilled, promise.State);
        }

        [TestMethod]
        public void Should_Adopt_Promise_Fulfilled_State()
        {
            var promise1 = Promise.Resolved(2);
            var promise2 = Promise.Resolved(promise1);
            Assert.AreEqual(PromiseState.Fulfilled, promise2.State);
            Assert.AreEqual(2, promise2.End());
        }

        [TestMethod]
        public void Should_Adopt_Promise_Rejected_State()
        {
            var promise1 = Promise<int>.Rejected(new Exception("Boo"));
            var promise2 = Promise.Resolved(promise1);
            Assert.AreEqual(PromiseState.Rejected, promise2.State);
            try
            {
                promise2.End();
                Assert.Fail("Should fail");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Boo", ex.Message);                
            }
        }

        [TestMethod]
        public void Should_Fulfill_Synchronously_Using_APM()
        {
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true));
            Assert.AreEqual("Hello", promise.End());
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Reject_Synchronously_Using_APM()
        {
            var promise = new Promise<object>((resolve, reject) => 
                reject(new Exception("Rejected"), true));
            try
            {
                promise.End();
                Assert.Fail("Should have raised exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Rejected", ex.Message);
            }
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Fulfill_Synchronously_Using_callbacks()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true));
            promise.Then((r, s) => {
                 Assert.AreEqual("Hello", r);
                 ++called;
            });
            promise.Then((r, s) => {
               Assert.AreEqual("Hello", r);
               ++called; 
            });
            Assert.AreEqual(2, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Fulfill_Asynchronously_Using_callbacks()
        {
            TestRunner.MTA(() =>
            {
                var called = 0;
                var promise = new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)));
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
                    Assert.IsFalse(promise.CompletedSynchronously);
                }
                else
                    Assert.Fail("Operation timed out");
            });
        }

        [TestMethod]
        public void Should_Reject_Synchronously_Using_Callbacks()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) => 
                reject(new Exception("Rejected"), true));
            promise.Catch((ex,s) => {
                Assert.AreEqual("Rejected", ex.Message);
                ++called;
            });
            promise.Catch((ex, s) => {
                Assert.AreEqual("Rejected", ex.Message);
                ++called;
            }); 
            Assert.AreEqual(2, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Skip_Reject_Specific_Synchronously_Using_Callbacks()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) =>
                reject(new ArgumentException("Bad name", "name"), true));
            promise.Catch<InvalidOperationException>((ex, s) =>
            {
                called += 1;
            })
            .Catch((ex, s) => {
                called += 2; 
            });
            Assert.AreEqual(2, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Reject_Specific_Synchronously_Using_Callbacks()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) =>
                reject(new ArgumentException("Bad name", "name"), true));
            promise.Catch<ArgumentException>((ex, s) =>
            {
                Assert.AreEqual("name", ex.ParamName);
                called += 1;
            })
            .Catch((ex, s) => {
                called += 2;               
            });
            Assert.AreEqual(1, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Reject_Asynchronously_Using_Callbacks()
        {
            TestRunner.MTA(() =>
            {
                var called = 0;
                var promise = new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ =>
                        reject(new Exception("Rejected"), false)));
                var catch1 = promise.Catch((ex, s) =>
                {
                    Assert.AreEqual("Rejected", ex.Message);
                    ++called;
                });
                var catch2 = promise.Catch((ex, s) =>
                {
                    Assert.AreEqual("Rejected", ex.Message);
                    ++called;
                });
                if (WaitHandle.WaitAll(
                    new[] {catch1.AsyncWaitHandle, catch2.AsyncWaitHandle}, 5.Sec()))
                {
                    Assert.AreEqual(2, called);
                    Assert.IsFalse(promise.CompletedSynchronously);
                }
                else
                    Assert.Fail("Operation timed out");
            });
        }

        [TestMethod]
        public void Should_Fulfill_Only_Once()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) => {
                    resolve("Hello", true);
                    resolve("Goodbye", true);
                })
                .Then((r,s) => {
                    Assert.AreEqual("Hello", r);
                    ++called;
                });
            Assert.AreEqual(1, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Reject_Only_Once()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) => {
                    reject(new Exception("Rejected"), true);
                    reject(new Exception("Uh oh!!"), true);
                })
                .Catch((ex,s) =>
                {
                    Assert.AreEqual("Rejected", ex.Message);
                    ++called;
                });
            Assert.AreEqual(1, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Propogate_Simple_Fulfilled_Callback()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true));
            promise.Catch((ex, s) => {})
                .Then((r, s) => {
                    Assert.AreEqual("Hello", r);
                    ++called;
                });
            Assert.AreEqual(1, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Propogate_Promise_Fulfilled_Callback()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true));
            promise.Catch((ex, s) => Promise.Resolved("Goodbye"))
                .Then((r, s) =>
                {
                    Assert.AreEqual("Hello", r);
                    ++called;
                });
            Assert.AreEqual(1, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Propogate_Exceptions_In_Fulfilled_Callback()
        {
            var called = 0;
            var verify = false;
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true));
            promise.Then((r, s) => {
                Assert.AreEqual("Hello", r);
                ++called; 
            });
            promise.Then((r,s) => { throw new Exception("Bad"); })
                .Then((r, s) => { Assert.Fail("Should skip"); return 12;})
                .Catch((ex, s) =>
                {
                    Assert.AreEqual("Bad", ex.Message);
                    ++called;
                })
                .Then((r,s) => verify = true);
            Assert.AreEqual(2, called);
            Assert.IsTrue(verify);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Propogate_Exceptions_In_Async_Fulfilled_Callback()
        {
            var called = false;
            var verify = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)))
                .Then((r,s) => { throw new Exception("Bad"); })
                .Then((r,s) => Assert.Fail("Should skip"))
                .Catch((ex, s) =>
                {
                    Assert.AreEqual("Bad", ex.Message);
                    called = true;
                })
                .Then((r, s) => verify = true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(verify);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Propogate_Exceptions_In_Rejected_Callback()
        {
            var called = false;
            var verify = false;
            var promise = new Promise<object>((resolve, reject) =>
                reject(new Exception("Foo"), true))
                .Then(null, (ex, s) => { throw new Exception("Bar"); })
                .Then((r,s) => Assert.Fail("Should skip"))
                .Catch((ex, s) =>
                {
                    Assert.AreEqual("Bar", ex.Message);
                    called = true;
                })
                .Then((r, s) => verify = true);
            Assert.IsTrue(called);
            Assert.IsTrue(verify);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Propogate_Exceptions_In_Async_Rejected_Callback()
        {
            var called = false;
            var verify = true;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ =>
                    reject(new Exception("Rejected"), false)))
                .Then(null, (ex, s) => { throw new Exception("Bar"); })
                .Then((r,s) => Assert.Fail("Should skip"))
                .Catch((ex, s) =>
                {
                    Assert.AreEqual("Bar", ex.Message);
                    called = true;
                })
                .Then((r, s) => verify = true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(verify);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Filter_Resolve_Synchronously_Using_APM()
        {
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true))
                .Then((r,s) => 12);
            Assert.AreEqual(12, promise.End());
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Filter_Reject_Synchronously_Using_APM()
        {
            var promise = new Promise<object>((resolve, reject) =>
                reject(new Exception("Rejected"), true))
                .Then((r,s) => 12, (ex,s) => { throw new Exception("Yeah"); });
            try
            {
                promise.End();
                Assert.Fail("Should have raised exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Yeah", ex.Message);
            }
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Filter_Reject_Asynchronously_Using_Callbacks()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ =>
                    reject(new Exception("Rejected"), false)))
                .Then((r,s) => 12, (exx,s) => { throw new Exception("Meltdown"); })
                .Catch((ex, s) =>
                {
                    Thread.Sleep(100);
                    Assert.AreEqual("Meltdown", ex.Message);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Filter_Resolve_Asynchronously_Using_callbacks()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)))
                .Then((r,s) => 12).Then((r,s) =>
                {
                    Thread.Sleep(100);
                    Assert.AreEqual(12, r);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Propogate_Exceptions_In_Filtered_Fulfilled_Callback()
        {
            var called = false;
            var verify = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)))
                .Then((r,s) => { throw new Exception("Bar"); })
                .Then((r,s) => Assert.Fail("Should skip"))
                .Catch((ex,s) =>
                {
                    Assert.AreEqual("Bar", ex.Message);
                    called = true;
                })
                .Then((r, s) => verify = true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(verify);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Propogate_Exceptions_In_Filtered_Rejected_Callback()
        {
            var called = false;
            var verify = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => reject(new Exception("Foo"), false)))
                .Then((r,s) => 12, (ex,s) => { throw new Exception("Bar"); })
                .Then((r,s) => Assert.Fail("Should skip"))
                .Catch((ex,s) =>
                {
                    Assert.AreEqual("Bar", ex.Message);
                    called = true;
                })
                .Then((r, s) => verify = true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(verify);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Pipe_Resolve_Synchronously_Using_APM()
        {
            var promise = new Promise<string>((resolve, reject) => resolve("Hello", true))
                .Then((r,s) => new Promise<int>((res, rej) => res(12, s)));
            Assert.AreEqual(12, promise.End());
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Pipe_Reject_Synchronously_Using_APM()
        {
            var promise = new Promise<string>((resolve, reject) => resolve("Hello", true))
                .Then((r,s) => new Promise<int>((res, rej) =>
                    rej(new Exception("Bad Data"), s)));
            try
            {
                promise.End();
                Assert.Fail("Should have raised exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Bad Data", ex.Message);
            }
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Pipe_Resolve_Asynchronously_Using_callbacks()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)))
                .Then((r,s) => new Promise<string>((res, rej) => res(r + " Craig", s)))
                
                .Then((r,s) =>
                {
                    Thread.Sleep(100);
                    Assert.AreEqual("Hello Craig", r);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Pipe_Reject_Asynchronously_Using_Callbacks()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)))
                .Then((r,s) => new Promise<int>((res, rej) =>
                    rej(new Exception("Bad Data"), s)))
                
                .Catch((ex, s) =>
                {
                    Thread.Sleep(100);
                    Assert.AreEqual("Bad Data", ex.Message);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Unwrap_Resolve_Synchronously_Using_APM()
        {
            var promise = new Promise<string>((resolve, reject) => resolve("Hello", true))
                .Then((r,s) => new Promise<int>((res, rej) => res(12, true)));
            Assert.AreEqual(12, promise.End());
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Unwrap_Reject_Synchronously_Using_APM()
        {
            var promise = new Promise<string>((resolve, reject) => resolve("Hello", true))
                .Then((r,s) => new Promise<int>((res, rej) =>
                    rej(new Exception("Bad Data"), true)))
                ;
            try
            {
                promise.End();
                Assert.Fail("Should have raised exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Bad Data", ex.Message);
            }
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Unwrap_Resolve_Asynchronously_Using_callbacks()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)))
                .Then((r,s) => new Promise<string>((res, rej) => res(r + " Craig", true)))               
                .Then((r,s) =>
                {
                    Thread.Sleep(100);
                    Assert.AreEqual("Hello Craig", r);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Unwrap_Reject_Asynchronously_Using_Callbacks()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)))
                .Then((r,s) => new Promise<int>((res, rej) =>
                    rej(new Exception("Bad Data"), true)))
                
                .Catch((ex, s) =>
                {
                    Thread.Sleep(100);
                    Assert.AreEqual("Bad Data", ex.Message);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Finalize_Fulfill_Synchronously_Using_callbacks()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true));
            promise.Finally(() =>
            {
                ++called;
            }).Then((r, s) => {
                Assert.AreEqual("Hello", r);
                ++called;
            });
            Assert.AreEqual(2, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Propogate_Finalize_Fulfill_Exception_Synchronously_Using_callbacks()
        {
            var called = 0;
            var promise = new Promise<string>((resolve, reject) => resolve("Hello", true));
            promise.Finally(() =>
            {
                ++called;
                throw new InvalidOperationException("Bad Data");
            }).Catch((ex, s) =>
            {
                Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                Assert.AreEqual("Bad Data", ex.Message);
                ++called;
            });
            Assert.AreEqual(2, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Finalize_Reject_Synchronously_Using_Callbacks()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) =>
                reject(new Exception("Rejected"), true));
            promise.Finally(() =>
            {
                ++called;
            })
            .Catch((ex, s) => {
                Assert.AreEqual("Rejected", ex.Message);
                ++called;
            });
            Assert.AreEqual(2, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Propogate_Finalize_Reject_Exception_Synchronously_Using_Callbacks()
        {
            var called = 0;
            var promise = new Promise<object>((resolve, reject) =>
                reject(new Exception("Rejected"), true));
            promise.Finally(() =>
            {
                ++called;
                throw new InvalidOperationException("Bad Data");
            })
            .Catch((ex, s) =>
            {
                Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                Assert.AreEqual("Bad Data", ex.Message);
                ++called;
            });
            Assert.AreEqual(2, called);
            Assert.IsTrue(promise.CompletedSynchronously);
        }

        [TestMethod]
        public void Should_Finalize_Fulfill_Asynchronously_Using_callbacks()
        {
            var called = 0;
            var promise = new Promise<string>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)));
            var final = promise.Finally(() =>
            {
                ++called;
            }).Then((r, s) =>
            {
                Assert.AreEqual("Hello", r);
                ++called;
            });
            if (final.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(2, called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Finalize_Reject_Asynchronously_Using_Callbacks()
        {
            var called  = 0;
            var promise = new Promise<object>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ =>
                    reject(new Exception("Rejected"), false)));
            var final = promise.Finally(() =>
            {
                ++called;
            })
            .Catch((ex, s) =>
            {
                Assert.AreEqual("Rejected", ex.Message);
                ++called;
            });
            if (final.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(2, called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Ignore_Fulfilled_Rejected_When_Cancelled()
        {
            var called = false;
            var cancel = false;
            Promise<object>.ResolveCallbackT fulfill = null;
            RejectCallback failed = null;
            var promise = new Promise<object>((resolve, reject) => {
                fulfill = resolve;
                failed  = reject;
            }).Then((result, s) => { called = true; })
              .Catch((ex, ss) => { called = true; })
              .Finally(() => cancel = true);
            promise.Cancel();
            fulfill("Hello", true);
            failed(new Exception(), true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsFalse(called);
                Assert.IsTrue(cancel);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Ignore_Fulfilled_Rejected_If_CancelledException()
        {
            var called  = false;
            var cancel  = false;
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true))
                .Then((result, s) => { throw new CancelledException(); })
                .Then((result, s) => { called = true; }, (ex, ss) => { called = true; });
            promise.Cancelled(ex => cancel = true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsFalse(called);
                Assert.IsTrue(cancel);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Call_Finally_When_Cancelled()
        {
            var called  = false;
            var cancel  = false;
            var promise = new Promise<object>((resolve, reject) => { })
                .Finally(() => { called = true; });
            promise.Cancel();
            promise.Cancelled(ex => cancel = true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
                Assert.IsTrue(cancel);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Call_Finally_If_CancelledException()
        {
            var called = false;
            var cancel = false;
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true))
            .Then((result, s) => { throw new CancelledException(); })
            .Finally(() => { called = true; });
            promise.Cancelled(ex => cancel = true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsTrue(called);
                Assert.IsTrue(cancel);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Wait_For_All_Promises_To_Fulfill()
        {
            var called = false;
            var promises = Enumerable.Range(0, 5).Select(
                i => new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ => resolve(i, false))))
               .ToArray();
            var all = Promise.All(promises).Then((results, s) => {
                Assert.AreEqual(promises.Length, results.Length);
                Assert.IsTrue(results.Cast<int>().SequenceEqual(
                    Enumerable.Range(0, promises.Length)));
                called = true;
            });
            if (all.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(all.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Reject_All_If_Any_Promise_Rejected()
        {
            var called = false;
            var promises = Enumerable.Range(0, 5).Select(
                i => new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ => {
                        if (i == 3)
                            reject(new Exception(i + " Failed"), false);
                        else
                            resolve(i, false); 
                })))
               .ToArray();
            var all = Promise.All(promises).Catch((ex, s) =>
            {
                Assert.AreEqual("3 Failed", ex.Message);
                called = true;
            });
            if (all.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(all.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Resolve_After_Delay()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var promise = Promise.Delay(.5.Sec()).Then((r, s) => stopwatch.Stop());
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 450);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Reject_If_Timeout_Elapsed()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) => {})
                .Timeout(.2.Sec())
                .Catch<TimeoutException>((ex, s) => {
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Cancel_Timeout_If_Resolved_Synchronously()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) => resolve("Hello", true))
                .Timeout(.3.Sec())
                .Then((r, s) => {
                    Assert.AreEqual("Hello", r);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }
        [TestMethod]
        public void Should_Cancel_Timeout_If_Rejected_Synchronously()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) =>
                reject(new Exception("Something went wrong"), true))
                .Timeout(.3.Sec())
                .Catch((ex, s) =>
                {
                    Assert.AreEqual("Something went wrong", ex.Message);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Cancel_Timeout_If_Resolved_Asynchronously()
        {
            var called = false;
            var promise = new Promise<string>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => Promise.Delay(.2.Sec())
                    .Then((r, s) => reject(new Exception("Something went wrong"), false))))
                .Timeout(.4.Sec())
                .Catch((ex, s) =>
                {
                    Assert.AreEqual("Something went wrong", ex.Message);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Cancel_Timeout_If_Rejected_Asynchronously()
        {
            var called = false;
            var promise = new Promise<string>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => Promise.Delay(.2.Sec())
                    .Then((r, s) => resolve("Hello", false))))
                .Timeout(.4.Sec())
                .Then((r, s) =>
                {
                    Assert.AreEqual("Hello", r);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Reject_If_Timeout_Elapsed_Asynchronously()
        {
            var called = false;
            var promise = new Promise<string>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => Promise.Delay(1.Sec())
                    .Then((r, s) => resolve("Hello", false))))
                .Timeout(.2.Sec())
                .Catch<TimeoutException>((ex, s) =>
                {
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Cancel_Parent_Timeout_Elapsed()
        {
            var called = false;
            var promise = new Promise<object>((resolve, reject) => { });

            var cancelled = promise.Timeout(.2.Sec())
                .Catch<TimeoutException>((ex, s) => {
                    called = true;
                });
            if (cancelled.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Translate_Failed_Actions_Into_Rejected_Promises()
        {
            var called = false;
            var promise = Promise.Try(() => {
                throw new InvalidOperationException("No connection");
            })
            .Catch((InvalidOperationException ex, bool s) =>
            {
                Assert.AreEqual("No connection", ex.Message);
                called = true;
            });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Translate_Functions_Into_Fulfilled_Promises()
        {
            var called = false;
            var promise = Promise.Try(() => 22)
            .Then((result, s) =>
            {
                Assert.AreEqual(22, result);
                called = true;
            });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Translate_Failed_Functions_Into_Rejected_Promises()
        {
            var called = false;
            var promise = Promise.Try(() =>
            {
                throw new InvalidOperationException("Out of paper");
            })
            .Catch((InvalidOperationException ex, bool s) =>
            {
                Assert.AreEqual("Out of paper", ex.Message);
                called = true;
            });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Chain_Promise_Functions_Into_Fulfilled_Promises()
        {
            var called = false;
            var promise = Promise.Try(() =>
                Promise.Delay(100.Millis()).Then((r,s) => 19))
            .Then((result, s) =>
            {
                Assert.AreEqual(19, result);
                called = true;
            });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Chain_Promise_Functions_Into_Rejected_Promises()
        {
            var called = false;
            var promise = Promise.Try(() =>
                Promise.Delay(100.Millis()).Then((r, s) => {
                    throw new ArgumentException("Bad param", "foo");
                }))
            .Catch((ArgumentException ex, bool s) =>
            {
                Assert.AreEqual("foo", ex.ParamName);
                called = true;
            });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }
    }
}
