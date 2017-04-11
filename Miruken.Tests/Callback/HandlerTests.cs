namespace Miruken.Tests.Callback
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;

    /// <summary>
    /// Summary description for HandlerTests
    /// </summary>
    [TestClass]
    public class HandlerTests
    {
        [TestMethod]
        public void Should_Indicate_Not_Handled()
        {
            var handler = new CustomHandler();
            Assert.IsFalse(handler.Handle(new Bee()));
        }

        [TestMethod]
        public void Should_Indicate_Not_Handled_Surrogate()
        {
            var handler = new Handler(new Controller());
            Assert.IsFalse(handler.Handle(new Bee()));
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Implicitly()
        {
            var foo     = new Foo();
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(foo));
            Assert.AreEqual(1, foo.Handled);
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Implicitly_Surrogate()
        {
            var foo     = new Foo();
            var handler = new Handler(new Controller());
            Assert.IsTrue(handler.Handle(foo));
            Assert.AreEqual(1, foo.Handled);
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Explicitly()
        {
            var bar     = new Bar();
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(bar));
            Assert.IsTrue(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
            Assert.IsFalse(handler.Handle(bar));
            Assert.AreEqual(2, bar.Handled);
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Contravariantly()
        {
            var foo     = new SuperFoo();
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(foo));
            Assert.IsTrue(foo.HasComposer);
            Assert.AreEqual(2, foo.Handled);
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Genericly()
        {
            var baz     = new Baz<int>(22);
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(baz));
            Assert.AreEqual(0, baz.Stuff);
            Assert.IsFalse(handler.Handle(new Baz<char>('M')));
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Genericly_Mapped()
        {
            var baz     = new Baz<int,float>(22,15.5f);
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(baz));
            Assert.AreEqual(0, baz.Stuff);
            Assert.AreEqual(0, baz.OtherStuff);
            Assert.IsFalse(handler.Handle(new Baz<char,float>('M',2)));
        }

        [TestMethod]
        public void Should_Handle_Callbacks_With_Keys()
        {
            var foo     = new Foo();
            var handler = new SpecialHandler();
            Assert.IsTrue(handler.Handle(foo, true));
            Assert.AreEqual(1, foo.Handled);
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException),
            "The policy for Miruken.Callback.HandlesAttribute rejected method 'Miruken.Tests.Callback.HandlerTests + BadHandler:Add'")
        ]
        public void Should_Reject_Handlers()
        {
            var handler = new BadHandler();
            handler.Handle(new Foo());
        }

        [TestMethod]
        public void Should_Indicate_Not_Provided()
        {
            var handler = new CustomHandler();
            var bee     = handler.Resolve<Bee>();
            Assert.IsNull(bee);
        }

        [TestMethod]
        public void Should_Indicate_Not_Provided_Surrogate()
        {
            var handler = new Handler(new Controller());
            var bee     = handler.Resolve<Bee>();
            Assert.IsNull(bee);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Implicitly()
        {
            var handler = new CustomHandler();
            var bar     = handler.Resolve<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Provide_Many_Callbacks_Implicitly()
        {
            var handler = new SpecialHandler();
            var bar     = handler.Resolve<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
            var bars    = handler.ResolveAll<Bar>();
            Assert.AreEqual(2, bars.Length);
            Assert.AreEqual(1, bars[0].Handled);
            Assert.IsFalse(bars[0].HasComposer);
            Assert.AreEqual(2, bars[1].Handled);
            Assert.IsFalse(bars[1].HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_By_Key()
        {
            var handler = new SpecialHandler();
            var boo     = handler.Resolve<Boo>();
            Assert.IsNotNull(boo);
            Assert.IsTrue(boo.HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Many_Callbacks_By_Key()
        {
            var handler = new SpecialHandler();
            var bees    = handler.ResolveAll<Bee>();
            Assert.AreEqual(3, bees.Length);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_With_Many_Keys()
        {
            var handler = new SpecialHandler();
            var baz1    = handler.Resolve<Baz<int>>();
            Assert.AreEqual(1, baz1.Stuff);
            var baz2    = handler.Resolve<Baz<string>>();
            Assert.AreEqual("Hello", baz2.Stuff);
            var baz3    = handler.Resolve<Baz<float>>();
            Assert.IsNull(baz3);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Implicitly_Surrogate()
        {
            var handler = new Handler(new Controller());
            var bar     = handler.Resolve<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Implicitly_With_Composer()
        {
            var handler = new CustomHandler();
            var boo     = handler.Resolve<Boo>();
            Assert.IsNotNull(boo);
            Assert.AreEqual(boo.GetType(), typeof(Boo));
            Assert.IsTrue(boo.HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Covariantly()
        {
            var handler = new CustomHandler();
            var bar     = handler.Resolve<SuperBar>();
            Assert.IsInstanceOfType(bar, typeof(SuperBar));
            Assert.IsTrue(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Greedily()
        {
            var handler = new CustomHandler().Chain(new CustomHandler());
            var bars    = handler.ResolveAll<Bar>();
            Assert.AreEqual(4, bars.Length);
            bars = handler.ResolveAll<SuperBar>();
            Assert.AreEqual(2, bars.Length);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Explicitly()
        {
            var handler = new CustomHandler();
            var baz     = handler.Resolve<Baz>();
            Assert.IsInstanceOfType(baz, typeof(SuperBaz));
            Assert.IsFalse(baz.HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Many_Callbacks_Explicitly()
        {
            var handler = new SpecialHandler();
            var baz     = handler.Resolve<Baz>();
            Assert.IsInstanceOfType(baz, typeof(SuperBaz));
            Assert.IsFalse(baz.HasComposer);
            var bazs    = handler.ResolveAll<Baz>();
            Assert.AreEqual(2, bazs.Length);
            Assert.IsInstanceOfType(bazs[0], typeof(SuperBaz));
            Assert.IsFalse(bazs[0].HasComposer);
            Assert.IsInstanceOfType(bazs[1], typeof(Baz));
            Assert.IsFalse(bazs[1].HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Generically()
        {
            var handler = new CustomHandler();
            var baz     = handler.Resolve<Baz<int>>();
            Assert.IsInstanceOfType(baz, typeof(Baz<int>));
            Assert.AreEqual(0, baz.Stuff);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Mapped()
        {
            var handler = new CustomHandler();
            var baz     = handler.Resolve<Baz<int,string>>();
            Assert.IsInstanceOfType(baz, typeof(Baz<int,string>));
            Assert.AreEqual(0, baz.Stuff);
        }

        [TestMethod]
        public void Should_Provide_All_Callbacks()
        {
            var handler = new CustomHandler();
            var bars    = handler.ResolveAll<Bar>();
            Assert.AreEqual(2, bars.Length);
        }

        [TestMethod]
        public void Should_Provide_Empty_Array_If_No_Matches()
        {
            var handler = new Handler();
            var bars    = handler.ResolveAll<Bar>();
            Assert.AreEqual(0, bars.Length);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_By_String_Key()
        {
            var handler = new CustomHandler();
            var bar     = handler.Resolve("Bar") as Bar;
            Assert.IsNotNull(bar);
        }

        [TestMethod,
             ExpectedException(typeof(InvalidOperationException),
                "The policy for Miruken.Callback.ProvidesAttribute rejected method 'Miruken.Tests.Callback.HandlerTests + BadProvider:Add'")
        ]
        public void Should_Reject_Providers()
        {
            var handler = new BadProvider();
            handler.Resolve<Foo>();
        }

        [TestMethod]
        public void Should_Compose_Callbacks()
        {
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(new Composition(new Foo())));
        }

        [TestMethod]
        public void Should_Resolve_Self_Implicitly()
        {
            var handler = new CustomHandler();
            var result  = handler.Resolve<CustomHandler>();
            Assert.AreSame(handler, result);
        }

        [TestMethod]
        public void Should_Resolve_Self_Implicitly_Decorated()
        {
            var handler = new CustomHandler();
            var result  = handler.Broadcast().Resolve<CustomHandler>();
            Assert.AreSame(handler, result);
        }

        [TestMethod]
        public void Should_Resolve_Self_Surrogate_Implicitly()
        {
            var controller = new Controller();
            var handler    = new Handler(controller);
            var result     = handler.Resolve<Controller>();
            Assert.AreSame(controller, result);
        }

        [TestMethod]
        public void Should_Resolve_Self_Surrogate_Implicitly_Decorated()
        {
            var controller = new Controller();
            var handler    = new Handler(controller);
            var result     = handler.Broadcast().Resolve<Controller>();
            Assert.AreSame(controller, result);
        }

        [TestMethod]
        public void Should_Resolve_Using_IServiceProvider()
        {
            var handler = (IServiceProvider)new CustomHandler();
            var bar     = (Bar)handler.GetService(typeof(Bar));
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Resolve_All()
        {
            var custom  = new CustomHandler();
            var special = new SpecialHandler();
            var handler = custom + special;
            var objects = handler.ResolveAll<object>();
            CollectionAssert.Contains(objects, custom);
            CollectionAssert.Contains(objects, special);
            CollectionAssert.Contains(objects, handler);
            Assert.AreEqual(12, objects.Length);
        }

        [TestMethod]
        public void Should_Broadcast_Callbacks()
        {
            var foo   = new Foo();
            var group = new CustomHandler()
                .Chain(new CustomHandler(), new CustomHandler());
            Assert.IsTrue(group.Broadcast().Handle(foo));
            Assert.AreEqual(3, foo.Handled);
        }

        [TestMethod]
        public void Should_Get_Target_If_Not_Decorated()
        {
            var handler = new Handler();
            Assert.AreSame(handler, HandlerDecorator.Decorated(handler, true));
        }

        [TestMethod]
        public void Should_Get_Deepest_Decorated_Handler()
        {
            var handler = new Handler();
            Assert.AreSame(handler, HandlerDecorator.Decorated(
                handler.Broadcast().BestEffort().Notify(), true));
        }

        [TestMethod]
        public void Should_Override_Providers()
        {
            var handler = new Handler();
            var foo     = handler.Provide(new Foo()).Resolve<Foo>();
            Assert.IsNotNull(foo);
        }

        [TestMethod]
        public void Should_Ignore_Providers()
        {
            var handler = new Handler();
            var foo     = handler.Provide(new Bar()).Resolve<Foo>();
            Assert.IsNull(foo);
        }

        private class Foo
        {     
            public int  Handled     { get; set; }
            public bool HasComposer { get; set; }
        }

        private class SuperFoo : Foo
        {
        }

        private class Bar
        {
            public int  Handled     { get; set; }
            public bool HasComposer { get; set; }

        }

        private class SuperBar : Bar
        {         
        }

        private class Boo
        {
            public bool HasComposer { get; set; }
        }

        private class Baz
        {
            public bool HasComposer { get; set; }
        }

        private class SuperBaz : Baz
        {          
        }

        private class Baz<T> : Baz
        {
            public Baz(T stuff)
            {
                Stuff = stuff;
            }
            public T Stuff { get; set; }
        }

        private class Baz<T,R> : Baz<T>
        {
            public Baz(T stuff, R otherStuff) : base(stuff)
            {
                OtherStuff = otherStuff;
            }

            public R OtherStuff { get; set; }
        }

        private class Bee
        {       
        }

        private class CustomHandler : Handler
        {
            [Handles]
            public void HandleFooImplict(Foo foo)
            {
                ++foo.Handled;
            }

            [Handles]
            public bool HandleSuperFooImplict(SuperFoo foo, IHandler composer)
            {
                ++foo.Handled;
                foo.HasComposer = true;
                return false;
            }

            [Handles]
            public bool HandleBarExplicit(Bar bar, IHandler composer)
            {
                ++bar.Handled;
                bar.HasComposer = true;
                return bar.Handled % 2 == 1;
            }

            [Handles]
            public bool HandlesGenericBaz<T>(Baz<T> baz)
            {
                if (typeof (T) == typeof (char)) 
                    return false;
                baz.Stuff = default(T);
                return true;
            }

            [Handles]
            public bool HandlesGenericBazMapping<R,T>(Baz<T,R> baz)
            {
                if (typeof(T) == typeof(char))
                    return false;
                baz.Stuff      = default(T);
                baz.OtherStuff = default(R);
                return true;
            }

            [Provides]
            public Bar ProvideBarImplicitly()
            {
                return new Bar { Handled = 1 };
            }

            [Provides]
            public Boo ProvideBooImplicitly(IHandler composer)
            {
                return new Boo { HasComposer = true };
            }

            [Provides]
            public SuperBar ProvideSuperBarImplicitly(IHandler composer)
            {
                return new SuperBar
                {
                    Handled     = 1,
                    HasComposer = true
                };
            }

            [Provides]
            public Baz ProvidesBazButIgnores()
            {
                return Unhandled<Baz>();
            }

            [Provides]
            public Baz<T> ProvidesBazGenerically<T>()
            {
                return new Baz<T>(default(T));
            }

            [Provides]
            public Baz<T,R> ProvidesBazMapped<R,T>()
            {
                return new Baz<T,R>(default(T), default(R));
            }

            [Provides]
            public void ProvideBazExplicitly(Resolution resolution, IHandler composer)
            {
               if (Equals(resolution.Key, typeof(Baz)))
                   resolution.Resolve(new SuperBaz(), composer);
            }

            [Provides("Foo"),
             Provides("Bar")]
            public object ProvidesByName(Resolution resolution)
            {
                switch (resolution.Key as string)
                {
                    case "Foo":
                        return new Foo();
                    case "Bar":
                        return new Bar();
                    default:
                        return null;
                }
            }
        }

        private class SpecialHandler : Handler
        {
            [Handles(typeof(Foo))]
            public void HandleFooKey(object cb)
            {
                var foo = (Foo)cb;
                ++foo.Handled;
            }

            [Provides(typeof(Boo))]
            public object ProvideBooKey(IHandler composer)
            {
                return new Boo { HasComposer = true };
            }

            [Provides]
            public Bar[] ProvideManyBar()
            {
                return new[]
                {
                    new Bar {Handled = 1},
                    new Bar {Handled = 2}
                };
            }

            [Provides(typeof(Bee))]
            public object ProvideManyBeeWithKey()
            {
                return new[]
                {
                    new Bee(), new Bee(), new Bee()
                };
            }

            [Provides(typeof(Baz<int>)),
             Provides(typeof(Baz<string>))]
            public object ProvideManyKeys(Resolution resolution)
            {
                if (Equals(resolution.Key, typeof(Baz<int>)))
                    return new Baz<int>(1);
                if (Equals(resolution.Key, typeof(Baz<string>)))
                    return new Baz<string>("Hello");
                return null;
            }

            [Provides]
            public void ProvideBazExplicitly(Resolution resolution, IHandler composer)
            {
                if (Equals(resolution.Key, typeof(Baz)))
                {
                    resolution.Resolve(new SuperBaz(), composer);
                    resolution.Resolve(new Baz(), composer);
                }
            }
        }

        private class BadHandler : Handler
        {
            [Handles]
            public int Add(int num1, int num2, int num3)
            {
                return num1 + num2 + num3;
            }
        }

        private class BadProvider : Handler
        {
            [Provides]
            public int Add(int num1, int num2, int num3)
            {
                return num1 + num2 + num3;
            }
        }

        private class Controller
        {
            [Handles]
            public void HandleFooImplict(Foo foo)
            {
                ++foo.Handled;
            }

            [Provides]
            public Bar ProvideBarImplicitly()
            {
                return new Bar { Handled = 1 };
            }
        }
    }
}
