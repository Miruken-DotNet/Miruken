namespace Miruken.Tests.Callback
{
    using System;
    using System.Linq;
    using Functional;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;

    [TestClass]
    public class TargetActionBuilderTests
    {
        private class Foo {}
        private class Bar<T> {}

        [TestMethod]
        public void Should_Create_No_Argument_Action()
        {
            var called = false;
            var target = new TargetActionBuilder<TargetActionBuilderTests, int>(action =>
            {
                Assert.IsTrue(action(this, _ => Array.Empty<object>()));
                called = true;
                return 22;
            });
            Assert.AreEqual(22, target.Invoke(_ => {}));
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Create_No_Argument_Void_Action()
        {
            var called = false;
            var target = new TargetActionBuilder<TargetActionBuilderTests>(action =>
            {
                Assert.IsTrue(action(this, _ => Array.Empty<object>()));
                called = true;
            });
            target.Invoke(_ => {});
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Create_One_Argument_Action()
        {
            var called = false;
            var foo    = new Foo();
            var target = new TargetActionBuilder<TargetActionBuilderTests, string>(action =>
            {
                Assert.IsTrue(action(this, args => new Handler().With(foo).ResolveArgs(args)));
                called = true;
                return "Hello";
            });
            Assert.AreEqual("Hello", target.Invoke((TargetActionBuilderTests _, Foo a) =>
            {
                Assert.IsTrue(Matches(Tuple.Create((object)a, (object)foo)));
            }));
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Create_One_Argument_Void_Action()
        {
            var called = false;
            var foo    = new Foo();
            var target = new TargetActionBuilder<TargetActionBuilderTests>(action =>
            {
                Assert.IsTrue(action(this, args => new Handler().With(foo).ResolveArgs(args)));
                called = true;
            });
            target.Invoke((TargetActionBuilderTests _, Foo a) =>
            {
                Assert.IsTrue(Matches(Tuple.Create((object)a, (object)foo)));
            });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Create_Two_Argument_Action()
        {
            var called = false;
            var foo    = new Foo();
            var bar    = new Bar<string>();
            var target = new TargetActionBuilder<TargetActionBuilderTests, double>(action =>
            {
                Assert.IsTrue(action(this, args => new Handler().With(foo).With(bar).ResolveArgs(args)));
                called = true;
                return 3.5;
            });
            Assert.AreEqual(3.5, target.Invoke((TargetActionBuilderTests _, Foo a, Bar<string> b) =>
            {
                Assert.IsTrue(Matches(
                    Tuple.Create((object)a, (object)foo),
                    Tuple.Create((object)b, (object)bar)));
            }));
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Create_Two_Argument_Void_Action()
        {
            var called = false;
            var foo    = new Foo();
            var bar    = new Bar<string>();
            var target = new TargetActionBuilder<TargetActionBuilderTests>(action =>
            {
                Assert.IsTrue(action(this, args => new Handler().With(foo).With(bar).ResolveArgs(args)));
                called = true;
            });
            target.Invoke((TargetActionBuilderTests _, Foo a, Bar<string> b) =>
            {
                Assert.IsTrue(Matches(
                    Tuple.Create((object)a, (object)foo),
                    Tuple.Create((object)b, (object)bar)));
            });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Create_Optional_Argument_Action()
        {
            var called = false;
            var foo    = new Foo();
            var target = new TargetActionBuilder<TargetActionBuilderTests>(action =>
            {
                Assert.IsTrue(action(this, args => new Handler().With(foo).ResolveArgs(args)));
                called = true;
            });
            target.Invoke((TargetActionBuilderTests _, Maybe<Foo> a) =>
            {
                Assert.IsTrue(Matches(Tuple.Create((object)a.Value, (object)foo)));
            });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Create_Empty_Optional_Argument_Action()
        {
            var called = false;
            var target = new TargetActionBuilder<TargetActionBuilderTests>(action =>
            {
                Assert.IsTrue(action(this, args => new Handler().ResolveArgs(args)));
                called = true;
            });
            target.Invoke((TargetActionBuilderTests _, Maybe<Foo> a) =>
            {
                Assert.IsFalse(a.HasValue);
            });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Reject_Action_If_Arg_Not_Resolved()
        {
            var called = false;
            var target = new TargetActionBuilder<TargetActionBuilderTests>(action =>
            {
                Assert.IsFalse(action(this, args => new Handler().ResolveArgs(args)));
                called = true;
            });
            target.Invoke((TargetActionBuilderTests _, Foo a) => {});
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Should_Call_Two_Argument_Action()
        {
            var foo    = new Foo();
            var bar    = new Bar<string>();
            var called = new Handler().With(foo).With(bar).Target()
                .Invoke((IHandler handler, Foo a, Bar<string> b) =>
                {
                    Assert.IsTrue(Matches(
                        Tuple.Create((object) a, (object) foo),
                        Tuple.Create((object) b, (object) bar)));
                    return true;
                });
            Assert.IsTrue(called.Item2.Value);
        }

        [TestMethod]
        public void Should_Call_Optional_Argument_Action()
        {
            var foo    = new Foo();
            var called = new Handler().With(foo).Target()
                .Invoke((IHandler handler, Maybe<Foo> a) =>
                {
                    Assert.IsTrue(Matches(
                        Tuple.Create((object)a.Value, (object)foo)));
                    return true;
                });
            Assert.IsTrue(called.Item2.Value);
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Should_Reject_Missing_Argument_Action()
        {
            new Handler().Target().Invoke((IHandler handler, Foo a) => {});
        }

        private static bool Matches(params Tuple<object, object>[] args) =>
            args.All(arg => ReferenceEquals(arg.Item1, arg.Item2));
    }
}
