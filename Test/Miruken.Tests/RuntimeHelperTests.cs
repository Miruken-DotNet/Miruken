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

            public static void HandleStatic()
            {
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

            public static string StaticProvideOne(int arg)
            {
                return $"Static {arg}";
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
            var toplevel = typeof(Bar).GetTopLevelInterfaces();
            CollectionAssert.AreEqual(toplevel, new [] { typeof(IBar) });
            toplevel = typeof(Baz).GetTopLevelInterfaces();
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
            var call = (Action<object>)RuntimeHelper.CompileMethod(
                typeof(Handler).GetMethod(nameof(Handler.Handle)),
                typeof(Action<object>));
            var handler = new Handler();
            call(handler);
        }

        [TestMethod]
        public void Should_Create_Single_Arg_Action()
        {
            var call = (Action<object, object>)RuntimeHelper.CompileMethod(
                typeof(Handler).GetMethod(nameof(Handler.HandleOne)),
                typeof(Action<object, object>));
            var handler = new Handler();
            call(handler, "Hello");
            Assert.AreEqual("Hello", handler.Arg1);
        }

        [TestMethod]
        public void Should_Create_Double_Arg_Action()
        {
            var call = (Action<object, object, object>)
                RuntimeHelper.CompileMethod(
                typeof(Handler).GetMethod(nameof(Handler.HandleTwo)),
                typeof(Action<object, object, object>));
            var handler = new Handler();
            call(handler, false, new DateTime(2007, 6, 14));
            Assert.AreEqual(false, handler.Arg1);
            Assert.AreEqual(new DateTime(2007, 6, 14), handler.Arg2);
        }

        [TestMethod]
        public void Should_Create_Triple_Arg_Action()
        {
            var call = (Action<object, object, object, object>)
                RuntimeHelper.CompileMethod(
                typeof(Handler).GetMethod(nameof(Handler.HandleThree)),
                typeof(Action<object, object, object, object>));
            var handler = new Handler();
            call(handler, false, new DateTime(2007, 6, 14), 22);
            Assert.AreEqual(false, handler.Arg1);
            Assert.AreEqual(new DateTime(2007, 6, 14), handler.Arg2);
            Assert.AreEqual(22, handler.Arg3);
        }

        [TestMethod]
        public void Should_Create_Static_No_Arg_Action()
        {
            var call = (Action)RuntimeHelper.CompileMethod(
                typeof(Handler).GetMethod(nameof(Handler.HandleStatic)),
                typeof(Action));
            call();
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_Action_Arg_Mismatch()
        {
            RuntimeHelper.CompileMethod(
                typeof(Handler).GetMethod(nameof(Handler.HandleTwo)),
                typeof(Func<object, object, object>));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Should_Fail_If_No_Args()
        {
            RuntimeHelper.CompileMethod(
                typeof(Handler).GetMethod(nameof(Handler.Handle)),
                typeof(Action<object, object>));
        }

        [TestMethod]
        public void Should_Create_No_Arg_Function()
        {
            var call = (Func<object, object>)
                RuntimeHelper.CompileMethod(
                typeof(Provider).GetMethod(nameof(Provider.Provide)),
                typeof(Func<object, object>));
            var provider = new Provider();
            Assert.AreEqual("Hello", call(provider));
        }

        [TestMethod]
        public void Should_Create_One_Arg_Function()
        {
            var call = (Func<object, object, object>)
                RuntimeHelper.CompileMethod(
                typeof(Provider).GetMethod(nameof(Provider.ProvideOne)),
                typeof(Func<object, object, object>));
            var provider = new Provider();
            Assert.AreEqual("22", call(provider, 22));
        }

        [TestMethod]
        public void Should_Create_Two_Args_Function()
        {
            var call = (Func<object, object, object, object>)
                RuntimeHelper.CompileMethod(
                typeof(Provider).GetMethod(nameof(Provider.ProvideTwo)),
                typeof(Func<object, object, object, object>));
            var provider = new Provider();
            Assert.AreEqual(new DateTime(2003, 4, 9),
                call(provider, 2, new DateTime(2003, 4, 7)));
        }

        [TestMethod]
        public void Should_Create_Three_Args_Function()
        {
            var call = (Func<object, object, object, object, object>)
                RuntimeHelper.CompileMethod(
                typeof(Provider).GetMethod(nameof(Provider.ProvideThree)),
                typeof(Func<object, object, object, object, object>));
            var provider = new Provider();
            Assert.AreEqual(-5.5, call(provider, 2, 3.5f, true));
        }

        [TestMethod]
        public void Should_sCreate_No_Arg_Constructor()
        {
            var ctor = (Func<object>)RuntimeHelper.CompileConstructor(
                typeof(Provider).GetConstructor(Type.EmptyTypes),
                typeof(Func<object>));
            var provider = ctor() as Provider;
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Should_Create_One_Arg_Constructor()
        {
            var ctor = (Func<object, object>)RuntimeHelper.CompileConstructor(
                typeof(Service).GetConstructor(new[] { typeof(IFoo) }),
                typeof(Func<object, object>));
            var baz = new Baz();
            var service = ctor(baz) as Service;
            Assert.IsNotNull(service);
            Assert.AreSame(baz, service.Foo);
        }

        [TestMethod]
        public void Should_Create_Static_One_Arg_Function()
        {
            var call = (Func<object, object>)RuntimeHelper.CompileMethod(
                typeof(Provider).GetMethod(nameof(Provider.StaticProvideOne)),
                typeof(Func<object, object>));
            Assert.AreEqual("Static 28", call(28));
        }
    }
}
