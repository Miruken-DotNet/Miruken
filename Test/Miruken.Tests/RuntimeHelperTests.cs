namespace Miruken.Tests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Infrastructure;

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

        public class Service
        {
            public Service(IFoo foo)
            {
                Foo = foo;
            }

            public IFoo Foo { get; }
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
            Assert.IsTrue(typeof(IBar).IsTopLevelInterface(typeof(Bar)));
            Assert.IsTrue(typeof(IBaz).IsTopLevelInterface(typeof(Baz)));
            Assert.IsTrue(typeof(IBoo).IsTopLevelInterface(typeof(Baz)));
            Assert.IsFalse(typeof(IBar).IsTopLevelInterface(typeof(Baz)));
            Assert.IsFalse(typeof(IFoo).IsTopLevelInterface(typeof(Bar)));
            Assert.IsFalse(typeof(IFoo).IsTopLevelInterface(typeof(Baz)));
        }

        [TestMethod]
        public void Should_Create_No_Arg_Action()
        {
            var call = RuntimeHelper.CreateCall<NoArgsDelegate>(
                typeof(Handler).GetMethod(nameof(Handler.Handle)));
            var handler = new Handler();
            call(handler);
        }

        [TestMethod]
        public void Should_Create_Single_Arg_Action()
        {
            var call = RuntimeHelper.CreateCallOneArg(
                typeof(Handler).GetMethod(nameof(Handler.HandleOne)));
            var handler = new Handler();
            call(handler, "Hello");
            Assert.AreEqual("Hello", handler.Arg1);
        }

        [TestMethod]
        public void Should_Create_Double_Arg_Action()
        {
            var call = RuntimeHelper.CreateCallTwoArgs(
                typeof(Handler).GetMethod(nameof(Handler.HandleTwo)));
            var handler = new Handler();
            call(handler, false, new DateTime(2007, 6, 14));
            Assert.AreEqual(false, handler.Arg1);
            Assert.AreEqual(new DateTime(2007, 6, 14), handler.Arg2);
        }

        [TestMethod]
        public void Should_Create_Triple_Arg_Action()
        {
            var call = RuntimeHelper.CreateCallThreeArgs(
                typeof(Handler).GetMethod(nameof(Handler.HandleThree)));
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
                typeof(Handler).GetMethod(nameof(Handler.HandleTwo)));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_No_Args()
        {
            RuntimeHelper.CreateCallOneArg(
                typeof(Handler).GetMethod(nameof(Handler.Handle)));
        }

        [TestMethod]
        public void Should_Create_No_Arg_Function()
        {
            var call = RuntimeHelper.CreateFuncNoArgs(
                typeof(Provider).GetMethod(nameof(Provider.Provide)));
            var provider = new Provider();
            Assert.AreEqual("Hello", call(provider));
        }

        [TestMethod]
        public void Should_Create_One_Arg_Function()
        {
            var call = RuntimeHelper.CreateFuncOneArg(
                typeof(Provider).GetMethod(nameof(Provider.ProvideOne)));
            var provider = new Provider();
            Assert.AreEqual("22", call(provider, 22));
        }

        [TestMethod]
        public void Should_Create_Two_Args_Function()
        {
            var call = RuntimeHelper.CreateFuncTwoArgs(
                typeof(Provider).GetMethod(nameof(Provider.ProvideTwo)));
            var provider = new Provider();
            Assert.AreEqual(new DateTime(2003, 4, 9),
                call(provider, 2, new DateTime(2003, 4, 7)));
        }

        [TestMethod]
        public void Should_Create_Three_Args_Function()
        {
            var call = RuntimeHelper.CreateFuncThreeArgs(
                typeof(Provider).GetMethod(nameof(Provider.ProvideThree)));
            var provider = new Provider();
            Assert.AreEqual(-5.5, call(provider, 2, 3.5f, true));
        }

        [TestMethod]
        public void Should_sCreate_No_Arg_Constructor()
        {
            var ctor = RuntimeHelper.CreateCtorNoArgs(
                typeof(Provider).GetConstructor(Type.EmptyTypes));
            var provider = ctor() as Provider;
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Should_Create_One_Arg_Constructor()
        {
            var ctor = RuntimeHelper.CreateCtorOneArg(
                typeof(Service).GetConstructor(new[] { typeof(IFoo) }));
            var baz = new Baz();
            var service = ctor(baz) as Service;
            Assert.IsNotNull(service);
            Assert.AreSame(baz, service.Foo);
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_Function_Arg_Mismatch()
        {
            RuntimeHelper.CreateFuncTwoArgs(
                typeof(Provider).GetMethod(nameof(Provider.ProvideOne)));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_No_ReturnType()
        {
            RuntimeHelper.CreateCallOneArg(
                typeof(Provider).GetMethod(nameof(Provider.ProvideVoid)));
        }
    }
}
