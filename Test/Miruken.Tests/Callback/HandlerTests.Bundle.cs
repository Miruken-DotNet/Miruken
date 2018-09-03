namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Concurrency;
    using static Protocol;

    [TestClass]
    public class HandlerBundleTests
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
        public void Should_Handle_Empty_Bundle()
        {
            Assert.IsTrue(_bowling.All(b => { }));
        }

        [TestMethod]
        public async Task Should_Handle_Empty_Bundle_Async()
        {
            var complete = await _bowling.AllAsync(b => { });
            Assert.IsTrue(complete);
        }

        [TestMethod]
        public void Should_Handle_All_Single_Bundle()
        {
            var handled  = false;
            var complete =_bowling.All(b => b
                .Add(h => handled = h.Handle(new ResetPins())));
            Assert.IsTrue(complete);
            Assert.IsTrue(handled);
        }

        [TestMethod]
        public async Task Should_Handle_All_Single_Bundle_Async()
        {
            var handled  = false;
            var complete = await _bowling.AllAsync(b => b
                .Add(h => handled = h.Handle(new ResetPins())));
            Assert.IsTrue(complete);
            Assert.IsTrue(handled);
        }

        [TestMethod]
        public void Should_Handle_All_Multiple_Bundle()
        {
            var         handled = false;
            var         pins    = new List<Pin>();
            BowlingBall ball    = null;
            var complete = _bowling.All(b => b
                .Add(h => handled = h.Handle(new ResetPins()))
                .Add(h => pins.AddRange(h.ResolveAll<Pin>()))
                .Add(h => ball = h.Command<BowlingBall>(new FindBowlingBall(10))));
            Assert.IsTrue(complete);
            Assert.IsTrue(handled);
            Assert.AreEqual(10, pins.Count);
            Assert.AreEqual(10, ball.Weight);
        }

        [TestMethod]
        public async Task Should_Handle_All_Multiple_Bundle_Async()
        {
            var handled = false;
            var pins = new List<Pin>();
            BowlingBall ball = null;
            var complete = await _bowling.AllAsync(b => b
                .Add(h => handled = h.Handle(new ResetPins()))
                .Add(h => pins.AddRange(h.ResolveAll<Pin>()))
                .Add(h => ball = h.Command<BowlingBall>(new FindBowlingBall(10))));
            Assert.IsTrue(complete);
            Assert.IsTrue(handled);
            Assert.AreEqual(10, pins.Count);
            Assert.AreEqual(10, ball.Weight);
        }

        [TestMethod]
        public void Should_Handle_All_Bundle()
        {
            var bowler   = new Bowler();
            var complete = _bowling.All(b => b
                .Add(h => h.Handle(new ResetPins()))
                .Add(h => h.ResolveAll<Pin>())
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(10)))
                .Add(h => h.Command<Bowler>(new TakeTurn(1, bowler))));
            Assert.IsTrue(complete);
            Assert.AreEqual(1, bowler.Frames[0].FirstTurn);
            Assert.AreEqual(1, bowler.Frames[0].SecondTurn);
        }

        [TestMethod]
        public async Task Should_Handle_All_Bundle_Async()
        {
            var bowler  = new Bowler();
            await _bowling.AllAsync(b => b
                .Add(h => h.Handle(new ResetPins()))
                .Add(h => h.ResolveAll<Pin>())
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(10)))
                .Add(h => h.Command<Bowler>(new TakeTurn(1, bowler))));
            Assert.AreEqual(1, bowler.Frames[0].FirstTurn);
            Assert.AreEqual(1, bowler.Frames[0].SecondTurn);
        }

        [TestMethod]
        public void Should_Identify_Incomplete_Bundle()
        {
            Assert.IsFalse(_bowling.All(b => b
                .Add(h => h.Handle(new ResetPins()))
                .Add(h => h.ResolveAll<Pin>())
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(30)))));
        }

        [TestMethod]
        public async Task Should_Identify_Incomplete_Bundle_Async()
        {
            var complete = await _bowling.AllAsync(b => b
                .Add(h => h.Handle(new ResetPins()))
                .Add(h => h.ResolveAll<Pin>())
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(30))));
            Assert.IsFalse(complete);
        }

        [TestMethod]
        public void Should_Handle_Any_Bundle()
        {
            var complete = _bowling.Any(b => b
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(30)))
                .Add(h => h.ResolveAll<Pin>()));
            Assert.IsTrue(complete);
        }

        [TestMethod]
        public async Task Should_Handle_Any_Bundle_Async()
        {
            var complete = await _bowling.AnyAsync(b => b
                .Add(h => h.Command<BowlingBall>(new FindBowlingBall(30)))
                .Add(h => h.ResolveAll<Pin>()));
            Assert.IsTrue(complete);
        }

        [TestMethod]
        public void Should_Support_Service_Provider()
        {
            Bowling bowling = null;
            var complete = _bowling.All(b => b
                .Add(h => bowling = h.Resolve<Bowling>()));
            Assert.IsTrue(complete);
            Assert.AreSame(_bowling, bowling);
        }

        [TestMethod]
        public async Task Should_Support_Service_Provider_Async()
        {
            Bowling bowling = null;
            var complete = await _bowling.AllAsync(b => b
                .Add(h => bowling = h.Resolve<Bowling>()));
            Assert.IsTrue(complete);
            Assert.AreSame(_bowling, bowling);
        }

        [TestMethod]
        public void Should_Support_Single_Protocol()
        {
            Frame frame  = null;
            var bowler   = new Bowler();
            var complete = _bowling.Any(b => b
                .Add(async h => frame = await Proxy<IBowling>(h).Bowl(1, bowler)));
            Assert.IsTrue(complete);
            Assert.AreEqual(1, frame.FirstTurn);
            Assert.AreEqual(1, frame.FirstTurn);
            Assert.AreSame(frame, bowler.Frames[0]);
        }

        [TestMethod]
        public async Task Should_Support_Single_Protocol_Async()
        {
            Frame frame  = null;
            var bowler   = new Bowler();
            var complete = await _bowling.AnyAsync(b => b
                .Add(async h => frame = await Proxy<IBowling>(h).Bowl(1, bowler)));
            Assert.IsTrue(complete);
            Assert.AreEqual(1, frame.FirstTurn);
            Assert.AreEqual(1, frame.FirstTurn);
            Assert.AreSame(frame, bowler.Frames[0]);
        }

        [TestMethod,
         ExpectedException(typeof(IndexOutOfRangeException))]
        public void Should_Propogate_Exceptions()
        {
            var bowler = new Bowler();
            _bowling.Any(b => b
                .Add(async h => await Proxy<IBowling>(h).Bowl(13, bowler)));
        }

        [TestMethod,
         ExpectedException(typeof(IndexOutOfRangeException))]
        public async Task Should_Propogate_Exceptions_Async()
        {
            var bowler = new Bowler();
            await _bowling.AnyAsync(b => b
                .Add(async h => await Proxy<IBowling>(h).Bowl(13, bowler)));
        }

        [TestMethod]
        public void Should_Reject_Unhandled_Protocol()
        {
            var bowler   = new Bowler();
            var complete = new Handler().Any(b => b
                .Add(async h => await Proxy<IBowling>(h).Bowl(7, bowler)));
            Assert.IsFalse(complete);
        }

        [TestMethod]
        public async Task Should_Reject_Unhandled_Protocol_Async()
        {
            var bowler   = new Bowler();
            var complete = await new Handler().AnyAsync(b => b
                .Add(async h => await Proxy<IBowling>(h).Bowl(7, bowler)));
            Assert.IsFalse(complete);
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException))]
        public async Task Should_Wrap_Exceptions_In_Promise()
        {
            var promise = _bowling.AnyAsync(b => b
                .Add(new Action<IHandler>(h =>
                    throw new ArgumentException("Bad value"))));
            await promise;
        }

        [TestMethod]
        public void Should_Support_Call_Semantics()
        {
            var bowler   = new Bowler();
            var complete = new Handler().BestEffort().Any(b => b
                .Add(async h => await Proxy<IBowling>(h).Bowl(8, bowler)));
            Assert.IsTrue(complete);
        }

        [TestMethod]
        public async Task Should_Support_Call_Semantics_Async()
        {
            var bowler   = new Bowler();
            var complete = await new Handler().BestEffort().AnyAsync(b => b
                .Add(async h => await Proxy<IBowling>(h).Bowl(8, bowler)));
            Assert.IsTrue(complete);
        }

        [TestMethod]
        public void Should_Support_Inner_Call_Semantics()
        {
            var bowler   = new Bowler();
            var complete = new HandlerAdapter(bowler).Any(b => b
                .Add(h => { Proxy<IBowling>(h.BestEffort()).Bowl(8, bowler); }));
            Assert.IsTrue(complete);
        }

        [TestMethod]
        public void Should_Handle_All_Multiple_Bundle_Greedily()
        {
            var handled  = 0;
            var pins     = new List<Pin>();
            var complete = _bowling.Broadcast().All(b => b
                .Add(h => handled += h.Handle(new ResetPins()) ? 1 : 0)
                .Add(h => pins.AddRange(h.ResolveAll<Pin>())));
            Assert.IsTrue(complete);
            Assert.AreEqual(5, handled);
            Assert.AreEqual(50, pins.Count);
        }

        [TestMethod]
        public async Task Should_Handle_All_Multiple_Bundle_Greedily_Async()
        {
            var handled  = 0;
            var pins     = new List<Pin>();
            var complete = await _bowling.Broadcast().AllAsync(b => b
                .Add(h => handled += h.Handle(new ResetPins()) ? 1 : 0)
                .Add(h => pins.AddRange(h.ResolveAll<Pin>())));
            Assert.IsTrue(complete);
            Assert.AreEqual(5, handled);
            Assert.AreEqual(50, pins.Count);
        }

        [TestMethod]
        public void Should_Handle_Any_Multiple_Bundle_Greedily()
        {
            var handled  = 0;
            var pins     = new List<Pin>();
            var complete = _bowling.Broadcast().Any(b => b
                .Add(h => handled += h.Handle(new ResetPins()) ? 1 : 0)
                .Add(h => pins.AddRange(h.ResolveAll<Pin>())));
            Assert.IsTrue(complete);
            Assert.AreEqual(5, handled);
            Assert.AreEqual(50, pins.Count);
        }

        [TestMethod]
        public async Task Should_Handle_Any_Multiple_Bundle_Greedily_Async()
        {
            var handled  = 0;
            var pins     = new List<Pin>();
            var complete = await _bowling.Broadcast().AnyAsync(b => b
                .Add(h => handled += h.Handle(new ResetPins()) ? 1 : 0)
                .Add(h => pins.AddRange(h.ResolveAll<Pin>())));
            Assert.IsTrue(complete);
            Assert.AreEqual(5, handled);
            Assert.AreEqual(50, pins.Count);
        }

        [TestMethod]
        public void Should_Support_Resolving_Bundles()
        {
            var handled      = false;
            var pins         = new List<Pin>();
            BowlingBall ball = null;
            HandlerDescriptor.GetDescriptor<Bowling>();
            HandlerDescriptor.GetDescriptor<Lane>();
            var complete = new BowlingProvider().Infer().All(b => b
                .Add(h => handled = h.Handle(new ResetPins()))
                .Add(h => pins.AddRange(h.ResolveAll<Pin>()))
                .Add(h => ball = h.Command<BowlingBall>(new FindBowlingBall(10))));
            Assert.IsTrue(complete);
            Assert.IsTrue(handled);
            Assert.AreEqual(10, pins.Count);
            Assert.AreEqual(10, ball.Weight);
        }

        private class BowlingProvider : Handler
        {
            [Provides]
            public Bowling Bowling { get; } = new Bowling();

            [Provides]
            public Lane[] Lanes { get; } = Enumerable.Range(0, 5)
                .Select(i => new Lane(true)).ToArray();
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
            private readonly bool _resolve;

            public Lane(bool resolve = false)
            {
                _resolve = resolve;
            }

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
                var h = _resolve ? composer.Infer() : composer;
                h.Command<BowlingBall>(new FindBowlingBall(5));
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
                var scope = Proxy<IBowling>(composer).GetScore(turn.Bowler);
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
