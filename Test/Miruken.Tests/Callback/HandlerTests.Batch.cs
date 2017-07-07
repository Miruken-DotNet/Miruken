namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Concurrency;

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
        public void Should_Handle_Single_Batch()
        {
            var handled = false;
            _bowling.All(b => b
                .Add(h => handled = h.Handle(new ResetPins())))
                .Wait();
            Assert.IsTrue(handled);
        }

        [TestMethod]
        public void Should_Handle_Multiple_Batch()
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
        public async Task Should_Handle_Async_Batch()
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
            private readonly Pin[] _pins =
                Enumerable.Range(0, 10)
                .Select(i => new Pin(i) {Up = true})
                .ToArray();

            [Provides]
            public Pin[] GetPins()
            {
                return _pins;
            }

            [Handles]
            public void Reset(ResetPins reset, IHandler composer)
            {
                Assert.IsNotNull(composer);
                foreach (var t in _pins) t.Up = true;
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

        private class Bowling : CompositeHandler
        {
            private readonly bool[] _pins
                = Enumerable.Repeat(true, 10).ToArray();

            [Handles]
            public BowlingBall FindBall(FindBowlingBall findBall)
            {
                return findBall.Weight < 20.0
                     ? new BowlingBall {Weight = findBall.Weight}
                     : null;
            }

            [Handles]
            public Promise<Bowler> TakeTurn(TakeTurn turn)
            {
                var frame  = turn.Frame;
                var bowler = turn.Bowler;
                bowler.Frames[frame - 1] = new Frame
                {
                    FirstTurn  = frame,
                    SecondTurn = frame
                };
                return Promise.Resolved(bowler);
            }
        }
    }
}
