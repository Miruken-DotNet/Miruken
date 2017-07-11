namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Concurrency;
    using static Protocol;

    [TestClass]
    public class HandlerBatchTests
    {
        private Bowling _bowling;

        [TestInitialize]
        public void Setup()
        {
            _bowling = new Bowling();
            for (var i = 0; i < 5; ++i)
                _bowling.AddHandlers(new Lane());
        }

        [TestMethod]
        public void Should_Handle_All_Single_Batch()
        {
            var handled = false;
            _bowling.All(b => b
                .Add(h => handled = h.Handle(new ResetPins())))
                .Wait();
            Assert.IsTrue(handled);
        }

        [TestMethod]
        public void Should_Handle_All_Multiple_Batch()
        {
            var         handled = false;
            var         pins    = new List<Pin>();
            BowlingBall ball    = null;
            _bowling.All(b => b
                .Add(h => handled = h.Handle(new ResetPins()))
                .Add(h => pins.AddRange(h.ResolveAll<Pin>()))
                .Add(h => ball = h.Command<BowlingBall>(new FindBowlingBall(10))))
                .Wait();
            Assert.IsTrue(handled);
            Assert.AreEqual(10, pins.Count);
            Assert.AreEqual(10, ball.Weight);
        }

        [TestMethod]
        public async Task Should_Handle_Async_All_Batch()
        {
            var bowler  = new Bowler();
            await _bowling.All(b => b
                .Add(h => h.Handle(new ResetPins()))
                .Add(h => h.ResolveAll<Pin>())
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(10)))
                .Add(h => h.Command<Bowler>(new TakeTurn(1, bowler))));
            Assert.AreEqual(1, bowler.Frames[0].FirstTurn);
            Assert.AreEqual(1, bowler.Frames[0].SecondTurn);
        }

        [TestMethod,
         ExpectedException(typeof(IncompleteBatchException))]
        public void Should_Fail_If_Batch_Incomplete()
        {
            _bowling.All(b => b
                .Add(h => h.Handle(new ResetPins()))
                .Add(h => h.ResolveAll<Pin>())
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(30))))
                .Wait();
        }

        [TestMethod]
        public void Should_Handle_Any_Batch()
        {
            _bowling.Any(b => b
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(30)))
                .Add(h => h.ResolveAll<Pin>()))
                .Wait();
        }

        [TestMethod]
        public void Should_Support_Service_Provider()
        {
            Bowling bowling = null;
            _bowling.All(b => b
                .Add(h => bowling = h.Resolve<Bowling>()))
                .Wait();
            Assert.AreSame(_bowling, bowling);
        }

        [TestMethod]
        public async Task Should_Support_Single_Protocol()
        {
            Frame frame = null;
            var bowler  = new Bowler();
            await _bowling.Any(b => b
                .Add(async h => frame = await P<IBowling>(h).Bowl(1, bowler)));
            Assert.AreEqual(1, frame.FirstTurn);
            Assert.AreEqual(1, frame.FirstTurn);
            Assert.AreSame(frame, bowler.Frames[0]);
        }

        [TestMethod,
         ExpectedException(typeof(IndexOutOfRangeException))]
        public async Task Should_Propogate_Exceptions()
        {
            var bowler = new Bowler();
            await _bowling.Any(b => b
                .Add(async h => await P<IBowling>(h).Bowl(13, bowler)));
        }

        [TestMethod,
         ExpectedException(typeof(IncompleteBatchException))]
        public async Task Should_Reject_Unhandled_Protocol()
        {
            var bowler = new Bowler();
            await new Handler().Any(b => b
                .Add(async h => await P<IBowling>(h).Bowl(7, bowler)));
        }

        [TestMethod]
        public async Task Should_Support_Call_Semantics()
        {
            var bowler = new Bowler();
            await new Handler().BestEffort().Any(b => b
                .Add(async h => await P<IBowling>(h).Bowl(8, bowler)));
        }

        [TestMethod]
        public void Should_Handle_All_Multiple_Batch_Greedily()
        {
            var handled = 0;
            var pins    = new List<Pin>();
            _bowling.Broadcast().All(b => b
                .Add(h => handled += h.Handle(new ResetPins()) ? 1 : 0)
                .Add(h => pins.AddRange(h.ResolveAll<Pin>())))
                .Wait();
            Assert.AreEqual(5, handled);
            Assert.AreEqual(50, pins.Count);
        }

        [TestMethod]
        public void Should_Handle_Any_Multiple_Batch_Greedily()
        {
            var handled = 0;
            var pins    = new List<Pin>();
            _bowling.Broadcast().Any(b => b
                .Add(h => handled += h.Handle(new ResetPins()) ? 1 : 0)
                .Add(h => pins.AddRange(h.ResolveAll<Pin>())))
                .Wait();
            Assert.AreEqual(5, handled);
            Assert.AreEqual(0, pins.Count);
        }

        private class Pin
        {
            public Pin(int number)
            {
                Number = number;
            }
            public int  Number { get;  }
            public bool Up     { get; set; }
        }

        private class Lane
        {
            [Provides]
            public Pin[] Pins { get; } =
                 Enumerable.Range(0, 10)
                .Select(i => new Pin(i) { Up = true })
                .ToArray();

            [Handles]
            public void Reset(ResetPins reset, IHandler composer)
            {
                Assert.IsNotNull(composer);
                foreach (var t in Pins) t.Up = true;
                composer.Command<BowlingBall>(new FindBowlingBall(5));
            }
        }

        private class Frame
        {
            public int FirstTurn  { get; set; }
            public int SecondTurn { get; set; }
        }

        private class Bowler
        {
            public Frame[] Frames { get; } = new Frame[11];
        }

        private class BowlingBall
        {
            public double Weight { get; set; }
        }

        private class FindBowlingBall
        {
            public FindBowlingBall(double weight)
            {
                Weight = weight;
            }
            public double Weight { get; }
        }

        private class TakeTurn
        {
            public TakeTurn(int frame, Bowler bowler)
            {
                Frame  = frame;
                Bowler = bowler;
            }
            public int    Frame  { get; }
            public Bowler Bowler { get; }
        }

        private class ResetPins { }

        private interface IBowling
        {
            Promise<Frame> Bowl(int frame, Bowler bowler);

            int GetScore(Bowler bowler);
        }

        private class Bowling : CompositeHandler, IBowling
        {
            [Handles]
            public BowlingBall FindBall(FindBowlingBall findBall)
            {
                return findBall.Weight < 20.0
                     ? new BowlingBall {Weight = findBall.Weight}
                     : null;
            }

            [Handles]
            public Promise<Bowler> TakeTurn(TakeTurn turn, IHandler composer)
            {
                var frame  = turn.Frame;
                var bowler = turn.Bowler;
                bowler.Frames[frame - 1] = new Frame
                {
                    FirstTurn  = frame,
                    SecondTurn = frame
                };
                var scope = P<IBowling>(composer).GetScore(turn.Bowler);
                return Promise.Resolved(bowler);
            }

            Promise<Frame> IBowling.Bowl(int frame, Bowler bowler)
            {
                if (frame > 11)
                    throw new IndexOutOfRangeException();

                var newFrame = new Frame
                {
                    FirstTurn  = frame,
                    SecondTurn = frame
                };
                bowler.Frames[frame - 1] = newFrame;
                return Promise.Resolved(newFrame);
            }

            public int GetScore(Bowler bowler)
            {
                return bowler.Frames
                    .Where(f => f != null)
                    .Aggregate(0, (total, f) =>
                    total + f.FirstTurn + f.SecondTurn);
            }
        }
    }
}
