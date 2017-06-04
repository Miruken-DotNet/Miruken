using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Infrastructure;

namespace Miruken.Tests
{
    [TestClass]
    public class RuntimeHelperTests
    {
        public class Handler
        {
            public object Arg1 { get; set; }
            public object Arg2 { get; set; }
            public object Arg3 { get; set; }

            public void Handle()
            {
            }

            public void HandleOne(string arg)
            {
                Arg1 = arg;
            }

            public void HandleTwo(bool arg1, DateTime arg2)
            {
                Arg1 = arg1;
                Arg2 = arg2;
            }

            public void HandleThree(bool arg1, DateTime arg2, int arg3)
            {
                Arg1 = arg1;
                Arg2 = arg2;
                Arg3 = arg3;
            }
        }

        public class Provider
        {
            public string Provide()
            {
                return "Hello";
            }

            public string ProvideOne(int arg)
            {
                return arg.ToString();
            }

            public DateTime ProvideTwo(int arg1, DateTime arg2)
            {
                return arg2.AddDays(arg1);
            }

            public double ProvideThree(int arg1, float arg2, bool arg3)
            {
                return (arg1 + arg2) * (arg3 ? -1 : 1);
            }

            public void ProvideVoid()
            {
            }
        }

        public interface IFoo {}
        public interface IBar {}
        public interface IBoo : IBar {}
        public interface IBaz : IFoo, IBar {}
        public class Bar : IBar {}
        public class Baz : IBaz, IBoo {}

        [TestMethod]
        public void Should_Get_Toplevel_Interfaces()
        {
            var toplevel = typeof(Bar).GetToplevelInterfaces();
            CollectionAssert.AreEqual(toplevel, new [] { typeof(IBar) });
            toplevel = typeof(Baz).GetToplevelInterfaces();
            CollectionAssert.AreEqual(toplevel, new[] { typeof(IBaz), typeof(IBoo) });
        }

        [TestMethod]
        public void Should_Determine_If_Toplevel_Interface()
        {
            Assert.IsTrue(RuntimeHelper.IsTopLevelInterface(typeof(IBar), typeof(Bar)));
            Assert.IsTrue(RuntimeHelper.IsTopLevelInterface(typeof(IBaz), typeof(Baz)));
            Assert.IsTrue(RuntimeHelper.IsTopLevelInterface(typeof(IBoo), typeof(Baz)));
            Assert.IsFalse(RuntimeHelper.IsTopLevelInterface(typeof(IBar), typeof(Baz)));
            Assert.IsFalse(RuntimeHelper.IsTopLevelInterface(typeof(IFoo), typeof(Bar)));
            Assert.IsFalse(RuntimeHelper.IsTopLevelInterface(typeof(IFoo), typeof(Baz)));
        }

        [TestMethod]
        public void Should_Create_Single_Arg_Action()
        {
            var call = RuntimeHelper.CreateCallOneArg(
                typeof(Handler).GetMethod("HandleOne"));
            var handler = new Handler();
            call(handler, "Hello");
            Assert.AreEqual("Hello", handler.Arg1);
        }

        [TestMethod]
        public void Should_Create_Double_Arg_Action()
        {
            var call = RuntimeHelper.CreateCallTwoArgs(
                typeof(Handler).GetMethod("HandleTwo"));
            var handler = new Handler();
            call(handler, false, new DateTime(2007, 6, 14));
            Assert.AreEqual(false, handler.Arg1);
            Assert.AreEqual(new DateTime(2007, 6, 14), handler.Arg2);
        }

        [TestMethod]
        public void Should_Create_Triple_Arg_Action()
        {
            var call = RuntimeHelper.CreateCallThreeArgs(
                typeof(Handler).GetMethod("HandleThree"));
            var handler = new Handler();
            call(handler, false, new DateTime(2007, 6, 14), 22);
            Assert.AreEqual(false, handler.Arg1);
            Assert.AreEqual(new DateTime(2007, 6, 14), handler.Arg2);
            Assert.AreEqual(22, handler.Arg3);
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_Action_Arg_Mismatch()
        {
            RuntimeHelper.CreateFuncOneArg(
                typeof(Handler).GetMethod("HandleTwo"));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_No_Args()
        {
            RuntimeHelper.CreateCallOneArg(
                typeof(Handler).GetMethod("Handle"));
        }

        [TestMethod]
        public void Should_Create_No_Arg_Function()
        {
            var call = RuntimeHelper.CreateFuncNoArgs(
                typeof(Provider).GetMethod("Provide"));
            var provider = new Provider();
            Assert.AreEqual("Hello", call(provider));
        }

        [TestMethod]
        public void Should_Create_One_Arg_Function()
        {
            var call = RuntimeHelper.CreateFuncOneArg(
                typeof(Provider).GetMethod("ProvideOne"));
            var provider = new Provider();
            Assert.AreEqual("22", call(provider, 22));
        }

        [TestMethod]
        public void Should_Create_Two_Args_Function()
        {
            var call = RuntimeHelper.CreateFuncTwoArgs(
                typeof(Provider).GetMethod("ProvideTwo"));
            var provider = new Provider();
            Assert.AreEqual(new DateTime(2003, 4, 9),
                call(provider, 2, new DateTime(2003, 4, 7)));
        }

        [TestMethod]
        public void Should_Create_Three_Args_Function()
        {
            var call = RuntimeHelper.CreateFuncThreeArgs(
                typeof(Provider).GetMethod("ProvideThree"));
            var provider = new Provider();
            Assert.AreEqual(-5.5, call(provider, 2, 3.5f, true));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_Function_Arg_Mismatch()
        {
            RuntimeHelper.CreateFuncTwoArgs(
                typeof(Provider).GetMethod("ProvideOne"));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_No_ReturnType()
        {
            RuntimeHelper.CreateCallOneArg(
                typeof(Provider).GetMethod("ProvideVoid"));
        }
    }
}
