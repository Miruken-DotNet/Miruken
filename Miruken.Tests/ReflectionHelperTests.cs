using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Infrastructure;

namespace Miruken.Tests
{
    [TestClass]
    public class ReflectionHelperTests
    {
        public class Handler
        {
            public object Arg1 { get; set; }
            public object Arg2 { get; set; }

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

            public void ProvideVoid()
            {
            }
        }

        [TestMethod]
        public void Should_Create_Single_Arg_Action()
        {
            var call = ReflectionHelper.CreateActionOneArg(
                typeof(Handler).GetMethod("HandleOne"));
            var handler = new Handler();
            call(handler, "Hello");
            Assert.AreEqual("Hello", handler.Arg1);
        }

        [TestMethod]
        public void Should_Create_Double_Arg_Action()
        {
            var call = ReflectionHelper.CreateActionTwoArgs(
                typeof(Handler).GetMethod("HandleTwo"));
            var handler = new Handler();
            call(handler, false, new DateTime(2007, 6, 14));
            Assert.AreEqual(false, handler.Arg1);
            Assert.AreEqual(new DateTime(2007, 6, 14), handler.Arg2);
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException),
             "Method HandlerTwo expects 2 argument(s)")]
        public void Should_Fail_If_Action_Arg_Mismatch()
        {
            ReflectionHelper.CreateFuncOneArg(
                typeof(Handler).GetMethod("HandleTwo"));
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException),
            "Method Handle expects 0 arguments")]
        public void Should_Fail_If_No_Args()
        {
            ReflectionHelper.CreateActionOneArg(
                typeof(Handler).GetMethod("Handle"));
        }

        [TestMethod]
        public void Should_Create_No_Arg_Function()
        {
            var call = ReflectionHelper.CreateFuncNoArgs(
                typeof(Provider).GetMethod("Provide"));
            var provider = new Provider();
            Assert.AreEqual("Hello", call(provider));
        }

        [TestMethod]
        public void Should_Create_One_Arg_Function()
        {
            var call = ReflectionHelper.CreateFuncOneArg(
                typeof(Provider).GetMethod("ProvideOne"));
            var provider = new Provider();
            Assert.AreEqual("22", call(provider, 22));
        }

        [TestMethod]
        public void Should_Create_Two_Args_Function()
        {
            var call = ReflectionHelper.CreateFuncTwoArgs(
                typeof(Provider).GetMethod("ProvideTwo"));
            var provider = new Provider();
            Assert.AreEqual(new DateTime(2003, 4, 9),
                call(provider, 2, new DateTime(2003, 4, 7)));
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException),
            "Method ProvideOne expects 1 argument(s)")]
        public void Should_Fail_If_Function_Arg_Mismatch()
        {
            ReflectionHelper.CreateFuncTwoArgs(
                typeof(Provider).GetMethod("ProvideOne"));
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException),
            "Method Provide is void")]
        public void Should_Fail_If_No_ReturnType()
        {
            ReflectionHelper.CreateActionOneArg(
                typeof(Provider).GetMethod("ProvideVoid"));
        }
    }
}
