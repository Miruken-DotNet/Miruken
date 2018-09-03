namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Concurrency;
    using Miruken.Infrastructure;

    [TestClass]
    public class HandlerBatchTests
    {
        private interface IEmailing
        {
            object Send(object message);
            Promise SendConfirm(object message);
            object Fail(object message);
            Promise FailConfirm(object message);
        }

        private interface IOffline : IEmailing
        {
        }

        private class EmailHandler : Handler, IEmailing
        {
            object IEmailing.Send(object message)
            {
                var batch = Composer.GetBatch<IEmailing, EmailBatch>();
                return batch != null
                    ? batch.Send(message)
                    : message;
            }

            Promise IEmailing.SendConfirm(object message)
            {
                var batch = Composer.GetBatch<IEmailing, EmailBatch>();
                return batch != null
                    ? batch.SendConfirm(message)
                    : Promise.Resolved(message);
            }

            object IEmailing.Fail(object message)
            {
                if (Equals(message, "OFF"))
                    return Composer.Proxy<IOffline>().Fail(message);
                throw new Exception("Can't send message");
            }

            Promise IEmailing.FailConfirm(object message)
            {
                var batch = Composer.GetBatch<IEmailing, EmailBatch>();
                return batch != null
                    ? batch.FailConfirm(message)
                    : Promise.Rejected(new Exception("Can'ts send message"));
            }
        }

        private class EmailBatch : IEmailing, IBatching
        {
            private readonly List<object> _messages  = new List<object>();
            private readonly List<Promise> _promises = new List<Promise>();
            private readonly List<Action> _resolves  = new List<Action>();

            object IEmailing.Send(object message)
            {
                _messages.Add(message + " batch");
                return null;
            }

            Promise IEmailing.SendConfirm(object message)
            {
                _messages.Add(message);
                var promise = new Promise<object>((resolve, reject) =>
                    _resolves.Add(() => resolve(message + " batch", false)));
                _promises.Add(promise);
                return promise;
            }

            object IEmailing.Fail(object message)
            {
                return Handler.Unhandled<object>();
            }

            Promise IEmailing.FailConfirm(object message)
            {
                var promise = new Promise<object>((resolve, reject) =>
                    _resolves.Add(() => reject(new Exception("Can't send message"), false)));
                _promises.Add(promise);
                return promise;
            }

            object IBatching.Complete(IHandler composer)
            {
                foreach (var resolve in _resolves)
                    resolve();
                var results = composer.Proxy<IEmailing>().Send(_messages);
                return _promises.Count > 0
                    ? Promise.All(_promises.ToArray()).Then((r, s) => results)
                    : results;
            }
        }

        [TestMethod]
        public void Should_Batch_Protocols()
        {
            var completed = false;
            var handler   = new EmailHandler();
            Assert.AreEqual("Hello", handler.Proxy<IEmailing>().Send("Hello"));
            handler.Batch(batch =>
            {
                Assert.IsNull(batch.Proxy<IEmailing>().Send("Hello"));
            }).Then((results, s) =>
            {
                Assert.AreEqual(1, results.Length);
                CollectionAssert.AreEqual(new[] { "Hello batch" }, (ICollection)results[0]);
                completed = true;
            });
            Assert.IsTrue(completed);
            Assert.AreEqual("Hello", handler.Proxy<IEmailing>().Send("Hello"));
        }

        [TestMethod]
        public void Should_Batch_Protocols_Promises()
        {
            var count   = 0;
            var handler = new EmailHandler();
            handler.Proxy<IEmailing>().SendConfirm("Hello").Then((r, s) =>
            {
                Assert.AreEqual("Hello", r);
                ++count;
            });
            var promise = handler.Batch(batch =>
                batch.Proxy<IEmailing>().SendConfirm("Hello").Then((r, s) =>
                {
                    Assert.AreEqual("Hello batch", r);
                    ++count;
                })
            ).Then((results, s) =>
            {
                Assert.AreEqual(1, results.Length);
                CollectionAssert.AreEqual(new[] { "Hello" }, (ICollection)results[0]);
                handler.Proxy<IEmailing>().SendConfirm("Hello").Then((r, ss) =>
                {
                    Assert.AreEqual("Hello", r);
                    ++count;
                });
            });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
                Assert.AreEqual(3, count);
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public async Task Should_Batch_Protocols_Async()
        {
            var handler = new EmailHandler();
            var result  = await handler.Proxy<IEmailing>().SendConfirm("Hello");
            Assert.AreEqual("Hello", result);
            var results = await handler.Batch(async batch =>
            {
                var confirm = await batch.Proxy<IEmailing>().SendConfirm("Hello");
                Assert.AreEqual("Hello batch", confirm);
            });
            Assert.AreEqual(1, results.Length);
            CollectionAssert.AreEqual(new[] { "Hello" }, (ICollection)results[0]);
            result = await handler.Proxy<IEmailing>().SendConfirm("Hello");
            Assert.AreEqual("Hello", result);
        }

        [TestMethod]
        public void Should_Reject_Batch_Protocol_Promises()
        {
            var count   = 0;
            var handler = new EmailHandler();
            handler.Batch(batch =>
                batch.Proxy<IEmailing>().FailConfirm("Hello")
                    .Catch((err, s) =>
                    {
                        Assert.AreEqual("Can't send message", err.Message);
                        ++count;
                    })
            )
            .Catch((err, s) =>
            {
                Assert.AreEqual("Can't send message", err.Message);
                handler.Proxy<IEmailing>().FailConfirm("Hello")
                    .Catch((errx, sx) =>
                    {
                        Assert.AreEqual("Can't send message", err.Message);
                        ++count;
                    });
            });
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task Should_Not_Batch_After_Await()
        {
            var count   = 0;
            var handler = new EmailHandler();
            var result = await handler.Proxy<IEmailing>().SendConfirm("Hello");
            Assert.AreEqual("Hello", result);
            var results = await handler.Batch(async batch =>
            {
                var confirm = await batch.Proxy<IEmailing>().SendConfirm("Hello");
                Assert.AreEqual("Hello batch", confirm);
                ++count;
                confirm = await batch.Proxy<IEmailing>().SendConfirm("Goodbye");
                Assert.AreEqual("Goodbye", confirm);
                ++count;
            });
            Assert.AreEqual(1, results.Length);
            CollectionAssert.AreEqual(new[] { "Hello" }, (ICollection)results[0]);
            result = await handler.Proxy<IEmailing>().SendConfirm("Hello");
            Assert.AreEqual("Hello", result);
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void Should_Batch_Requested_Protocols()
        {
            var completed = false;
            var handler   = new EmailHandler();
            handler.Batch<IEmailing>(batch =>
                Assert.IsNull(batch.Proxy<IEmailing>().Send("Hello")))
            .Then((results, s) =>
            {
                Assert.AreEqual(1, results.Length);
                CollectionAssert.AreEqual(new[] { "Hello batch" }, (ICollection)results[0]);
                completed = true;
            });
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Should_Batch_Requested_Protocols_Promises()
        {
            var count   = 0;
            var handler = new EmailHandler();
            handler.Proxy<IEmailing>().SendConfirm("Hello").Then((r, s) =>
            {
                Assert.AreEqual("Hello", r);
                ++count;
            });
            handler.Batch<IEmailing>(batch =>
            {
                batch.Proxy<IEmailing>().SendConfirm("Hello").Then((r, s) =>
                {
                    Assert.AreEqual("Hello batch", r);
                    ++count;
                });
            })
            .Then((results, s) =>
            {
                Assert.AreEqual(1, results.Length);
                CollectionAssert.AreEqual(new[] { "Hello" }, (ICollection)results[0]);
                handler.Proxy<IEmailing>().SendConfirm("Hello").Then((r, ss) =>
                {
                    Assert.AreEqual("Hello", r);
                    ++count;
                });
            });
            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public void Should_Not_Batch_Unrequested_Protocols()
        {
            var count   = 0;
            var handler = new EmailHandler();
            handler.Batch<IOffline>(batch =>
                batch.Proxy<IEmailing>().SendConfirm("Hello").Then((r, s) =>
                {
                    Assert.AreEqual("Hello", r);
                    ++count;
                })
            );
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void Should_Not_Batch_After_Completed_Async()
        {
            var count   = 0;
            var handler = new EmailHandler();
            handler.Batch(batch =>
                batch.Proxy<IEmailing>().SendConfirm("Hello").Then((r, s) =>
                    batch.Proxy<IEmailing>().SendConfirm("Hello").Then((rr, ss) =>
                    {
                        Assert.AreEqual("Hello", rr);
                        ++count;
                    }))
            );
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task Should_Supress_Batching()
        {
            var called  = false;
            var handler = new EmailHandler();
            var results = await handler.Batch(batch =>
            {
                Assert.AreEqual("Hello", batch.NoBatch()
                    .Proxy<IEmailing>().Send("Hello"));
                called = true;
            });
            Assert.IsTrue(called);
            Assert.AreEqual(0, results.Length);
        }

        [TestMethod]
        public async Task Should_Supress_Batching_When_Resolving()
        {
            var called  = false;
            var handler = new EmailHandler();
            var results = await handler.Batch(batch =>
            {
                Assert.AreEqual("Hello", batch.Infer().NoBatch()
                    .Proxy<IEmailing>().Send("Hello"));
                called = true;
            });
            Assert.IsTrue(called);
            Assert.AreEqual(0, results.Length);
        }

        [TestMethod]
        public void Should_Filter_Batches()
        {
            var count   = 0;
            var handler = new EmailHandler();
            handler.Aspect((cb,c,s) => ++count).Batch(batch =>
            {
                Assert.IsNull(batch.Proxy<IEmailing>().Send("Hello"));
            })
            .Then((results, s) =>
                CollectionAssert.AreEqual(new[] { "Hello batch" }, results));
            Assert.AreEqual(2, count);
        }
    }
}
