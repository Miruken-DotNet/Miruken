﻿namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Concurrency;
    using Miruken.Context;

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
        public void Should_Indicate_Not_Handled_Adapter()
        {
            var handler = new HandlerAdapter(new Controller());
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
        public void Should_Handle_Static_Callbacks_Implicitly()
        {
            HandlerDescriptor.GetDescriptor<CustomHandler>();
            var foo     = new Foo();
            var handler = new StaticHandler();
            Assert.IsTrue(handler.Handle(foo));
            Assert.AreEqual(1, foo.Handled);
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Implicitly_Adapter()
        {
            var foo     = new Foo();
            var handler = new HandlerAdapter(new Controller());
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
        public void Should_Handle_Callbacks_Implicitly_Genericly()
        {
            var baz     = new BazInt(29);
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(baz));
            Assert.AreEqual(0, baz.Stuff);
        }

        [TestMethod]
        public void Should_Handle_Arrays()
        {
            var handler = new ArrayHandler();
            var type    = handler.Command<string>(new[] { 1, 2, 3 });
            Assert.AreEqual("integers", type);
            type = handler.Command<string>(new[] { "red", "green", "blue" });
            Assert.AreEqual("string", type);
            type = handler.Command<string>(new[] { typeof(int), typeof(string)});
            Assert.AreEqual("types", type);
            type = handler.Command<string>(new[] { 'a', 'b', 'c' });
            Assert.AreEqual("array", type);
        }

        [TestMethod]
        public void Should_Provide_Arrays()
        {
            var handler = new ArrayHandler();
            Array array = handler.Resolve<int[]>();
            CollectionAssert.AreEqual(new [] { 2, 4, 6 }, array);
            array = handler.Resolve<string[]>();
            CollectionAssert.AreEqual(new[] { "square", "circle"}, array);
            array = handler.Resolve<Type[]>();
            CollectionAssert.AreEqual(new[] { typeof(float), typeof(object) }, array);
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
         ExpectedException(typeof(InvalidOperationException))]
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
        public void Should_Indicate_Not_Provided__Adapter()
        {
            var handler = new HandlerAdapter(new Controller());
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
        public async Task Should_Provide_Callbacks_Implicitly_Async()
        {
            var handler = new CustomAsyncHandler();
            var bar     = await handler.ResolveAsync<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Implicitly_Waiting()
        {
            var handler = new CustomAsyncHandler();
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
            Assert.AreEqual(3, bars.Length);
            Assert.AreEqual(1, bars[0].Handled);
            Assert.IsFalse(bars[0].HasComposer);
            Assert.AreEqual(2, bars[1].Handled);
            Assert.IsFalse(bars[1].HasComposer);
            Assert.AreEqual(3, bars[2].Handled);
            Assert.IsFalse(bars[2].HasComposer);
        }

        [TestMethod]
        public async Task Should_Provide_Many_Callbacks_Implicitly_Async()
        {
            var handler = new SpecialAsyncHandler();
            var bar     = await handler.ResolveAsync<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
            var bars = handler.ResolveAll<Bar>();
            Assert.AreEqual(3, bars.Length);
            Assert.AreEqual(1, bars[0].Handled);
            Assert.IsFalse(bars[0].HasComposer);
            Assert.AreEqual(2, bars[1].Handled);
            Assert.IsFalse(bars[1].HasComposer);
            Assert.AreEqual(3, bars[2].Handled);
            Assert.IsFalse(bars[2].HasComposer);
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
        public async Task Should_Provide_Callbacks_By_Key_Async()
        {
            var handler = new SpecialAsyncHandler();
            var boo     = await handler.ResolveAsync<Boo>();
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
        public async Task Should_Provide_Many_Callbacks_By_Key_Async()
        {
            var handler = new SpecialAsyncHandler();
            var bees    = await handler.ResolveAllAsync<Bee>();
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
        public async Task Should_Provide_Callbacks_With_Many_Keys_Async()
        {
            var handler = new SpecialAsyncHandler();
            var baz1    = await handler.ResolveAsync<Baz<int>>();
            Assert.AreEqual(1, baz1.Stuff);
            var baz2 = handler.Resolve<Baz<string>>();
            Assert.AreEqual("Hello", baz2.Stuff);
            var baz3 = handler.Resolve<Baz<float>>();
            Assert.IsNull(baz3);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Implicitly_Adapter()
        {
            var handler = new HandlerAdapter(new Controller());
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
            var handler = new CustomHandler() + new CustomHandler();
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

        [TestMethod]
        public void Should_Provide_Callbacks_By_String_Case_Insensitive_Key()
        {
            var handler = new CustomHandler();
            var boo     = handler.Resolve("boo") as Boo;
            Assert.IsNotNull(boo);
        }

        [TestMethod]
        public void Should_Provide_Callback_Using_Constraints()
        {
            var handler = new SpecialHandler();
            Assert.IsNotNull(handler.Resolve<Foo>());
            Assert.IsNotNull(handler.Resolve<SuperFoo>());
        }

        [TestMethod]
        public void Should_Skip_Methods_With_Unmatched_Constraints()
        {
            var handler = new SpecialHandler();
            Assert.IsNull(handler.Resolve<FooDecorator>());
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Should_Reject_Providers()
        {
            var handler = new BadProvider();
            handler.Resolve<Foo>();
        }

        [TestMethod]
        public async Task Should_Filter_Async_Resolution()
        {
            var handler = new CustomHandler();
            var bar     = await handler.Aspect((_, c) => true)
                                       .ResolveAsync<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Async_Filter_Resolution()
        {
            var handler = new CustomHandler();
            var bar     = handler.Aspect((_, c) => Promise.True)
                                 .Resolve<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public async Task Should_Async_Filter_Async_Resolution()
        {
            var handler = new CustomHandler();
            var bar     = await handler.Aspect((_,c) => Promise.True)
                                       .ResolveAsync<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod,
          ExpectedException(typeof(RejectedException))]
        public void Should_Async_Cancel_Resolution()
        {
            var handler = new CustomHandler();
            handler.Aspect((_, c) => Promise.False).Resolve<Bar>();
        }

        [TestMethod,
         ExpectedException(typeof(OperationCanceledException),
            AllowDerivedTypes = true)]
        public async Task Should_Cancel_Async_Resolution()
        {
            var handler = new CustomHandler();
            var promise = handler.Aspect((_, c) => false).ResolveAsync<Bar>();
            Assert.IsInstanceOfType(promise, typeof(Promise<Bar>));
            await promise;
        }

        [TestMethod,
         ExpectedException(typeof(OperationCanceledException),
            AllowDerivedTypes = true)]
        public async Task Should_Async_Cancel_Async_Resolution()
        {
            var handler = new CustomHandler();
            await handler.Aspect((_, c) => Promise.False)
                         .ResolveAsync<Bar>();
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
        public void Should_Resolve_Self_Adapter_Implicitly()
        {
            var controller = new Controller();
            var handler    = new HandlerAdapter(controller);
            var result     = handler.Resolve<Controller>();
            Assert.AreSame(controller, result);
        }

        [TestMethod]
        public void Should_Resolve_Self_Adapter_Implicitly_Decorated()
        {
            var controller = new Controller();
            var handler    = new HandlerAdapter(controller);
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
            Assert.AreEqual(12, objects.Length);
        }

        [TestMethod]
        public void Should_Broadcast_Callbacks()
        {
            var foo   = new Foo();
            var group = new CustomHandler()
                      + new CustomHandler()
                      + new CustomHandler();
            Assert.IsTrue(group.Broadcast().Handle(foo));
            Assert.AreEqual(3, foo.Handled);
        }

        [TestMethod]
        public void Should_Get_Target_If_Not_Decorated()
        {
            var handler = new Handler();
            Assert.AreSame(handler, handler.Decorated(true));
        }

        [TestMethod]
        public void Should_Get_Deepest_Decorated_Handler()
        {
            var handler = new Handler();
            Assert.AreSame(handler, handler.Broadcast().BestEffort()
                .Notify().Decorated(true));
        }

        [TestMethod]
        public void Should_Override_Providers()
        {
            var handler = new Handler();
            var foo     = handler.Provide(new Foo()).Resolve<Foo>();
            Assert.IsNotNull(foo);
        }

        [TestMethod]
        public void Should_Override_Providers_Many()
        {
            var foo1    = new Foo();
            var foo2    = new Foo();
            var handler = new Handler();
            var foos    = handler.Provide(new [] {foo1, foo2}).ResolveAll<Foo>();
            CollectionAssert.AreEqual(new [] {foo1, foo2}, foos);
        }

        [TestMethod]
        public void Should_Ignore_Providers()
        {
            var handler = new Handler();
            var foo     = handler.Provide(new Bar()).Resolve<Foo>();
            Assert.IsNull(foo);
        }

        [TestMethod]
        public void Should_Create_Filters()
        {
            var bar     = new Bar();
            var handler = new FilteredHandler();
            Assert.IsTrue(handler.Handle(bar));
            Assert.AreEqual(2, bar.Handled);
            Assert.AreEqual(2, bar.Filters.Count);
            Assert.AreSame(handler, bar.Filters[1]);
            Assert.IsInstanceOfType(bar.Filters[0], typeof(LogFilter<Bar, object>));
        }

        [TestMethod]
        public void Should_Skip_Filters()
        {
            var bee     = new Bee();
            var handler = new FilteredHandler();
            Assert.IsTrue(handler.Handle(bee));
            Assert.AreEqual(0, bee.Filters.Count);
        }

        [TestMethod]
        public void Should_Reject_Method_If_Skipping_Required_Filters()
        {
            var handler = new FilteredHandler();
            Assert.IsTrue(handler.Handle(new Bar()));
            Assert.IsFalse(handler.SkipFilters().Handle(new Bar()));
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public async Task Should_Propogate_Rejected_Filter_Promise()
        {
            var boo     = new Boo();
            var handler = new SpecialFilteredHandler() + new FilteredHandler();
            await handler.CommandAsync(boo);
        }

        [TestMethod]
        public void Should_Infer_Callback_Filter_Generic_Types()
        {
            var handler  = new FilterResolver();
            var provider = new FilterAttribute(typeof(RequestFilterCb<>));
            var filter   = provider.GetFilters(
                null, typeof(string), typeof(int), handler)
                .ToArray();
            Assert.AreEqual(typeof(RequestFilterCb<string>), handler.RequestedType);
        }

        [TestMethod]
        public void Should_Infer_Result_Filter_Generic_Types()
        {
            var handler  = new FilterResolver();
            var provider = new FilterAttribute(typeof(RequestFilterRes<>));
            var filter   = provider.GetFilters(
                null, typeof(string), typeof(int), handler)
                .ToArray();
            Assert.AreEqual(typeof(RequestFilterRes<int>), handler.RequestedType);
        }

        [TestMethod,
         ExpectedException(typeof(ArgumentException))]
        public void Should_Reject_Generic_Types_Not_Inferred()
        {
            var provider = new FilterAttribute(typeof(RequestFilterBad<>));
            Assert.IsNull(provider);
        }

        [TestMethod]
        public void Should_Ignore_Option_At_Boundary()
        {
            var handler = new Handler()
                .WithFilters(new LogFilter<string, int>());
            var options = new FilterOptions();
            Assert.IsTrue(handler.Handle(options));
            Assert.AreEqual(1, options.ExtraFilters.Length);
            Assert.IsFalse(handler.Break().Handle(new FilterOptions()));
        }

        [TestMethod]
        public void Should_Create_Implicitly()
        {
            var handler = new StaticHandler();
            HandlerDescriptor.GetDescriptor<Controller>();
            var instance = handler.Resolve<Controller>();
            Assert.IsNotNull(instance);
            Assert.AreNotSame(instance, handler.Resolve<Controller>());
        }

        [TestMethod]
        public void Should_Create_Generic_Implicitly()
        {
            var view = new Screen();
            var bar  = new SuperBar();
            HandlerDescriptor.GetDescriptor(typeof(Controller<,>));
            var controller = new StaticHandler()
                .Provide(view).Provide(bar)
                .Resolve<Controller<Screen, Bar>>();
            Assert.IsNotNull(controller);
            Assert.AreSame(view, controller.View);
            Assert.AreSame(bar, controller.Model);
        }

        [TestMethod]
        public void Should_Create_Resolving_Implicitly()
        {
            HandlerDescriptor.GetDescriptor<Controller>();
            Assert.IsTrue(new StaticHandler().Infer().Handle(new Foo()));
        }

        [TestMethod]
        public void Should_Create_Resolving_Generic_Implicitly()
        {
            var boo = new Boo();
            var baz = new SuperBaz();
            HandlerDescriptor.GetDescriptor<Controller>();
            HandlerDescriptor.GetDescriptor(typeof(Controller<,>));
            var instance = new StaticHandler().Infer()
                .Provide(boo).Provide(baz)
                .Resolve<Controller<Boo, Baz>>();
            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public void Should_Provide_Instance_Implicitly()
        {
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor<Controller>();
            var bar = new StaticHandler().Infer().Resolve<Bar>();
            Assert.IsNotNull(bar);
        }

        [TestMethod]
        public void Should_Provide_Dependencies_Implicitly()
        {
            var view = new Screen();
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor(typeof(Controller));
            HandlerDescriptor.GetDescriptor(typeof(Controller<,>));
            var instance = new StaticHandler().Infer()
                .Provide(view).Resolve<Controller<Screen, Bar>>();
            Assert.IsNotNull(instance);
            Assert.AreSame(view, instance.View);
        }

        [TestMethod]
        public void Should_Detect_Circular_Dependencies()
        {
            var view = new Screen();
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor(typeof(Controller<,>));
            var instance = new StaticHandler().Infer()
                .Provide(view).Resolve<Controller<Screen, Bar>>();
            Assert.IsNull(instance);
        }

        [TestMethod]
        public void Should_Create_Singletons_Implicitly()
        {
            var handler = new StaticHandler();
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor<Application>();
            var app = handler.Resolve<Application>();
            Assert.IsNotNull(app);
            Assert.AreSame(app, handler.Resolve<Application>());
        }

        [TestMethod]
        public void Should_Create_Generic_Singletons_Implicitly()
        {
            var view    = new Screen();
            var handler = new StaticHandler().Infer();
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor(typeof(Controller));
            HandlerDescriptor.GetDescriptor(typeof(Controller<,>));
            HandlerDescriptor.GetDescriptor(typeof(Application<>));
            var app1 = handler.Provide(view)
                .Resolve<Application<Controller<Screen, Bar>>>();
            Assert.IsNotNull(app1);
            Assert.AreSame(view, app1.RootController.View);
            Assert.AreSame(view, app1.MainScreen);
            var app2 = handler.Provide(view)
                .Resolve<Application<Controller<Screen, Bar>>>();
            Assert.AreSame(app1, app2);
        }

        [TestMethod]
        public void Should_Create_Contextual_Implicitly()
        {
            Screen screen;
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor<Screen>();
            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                screen = context.Resolve<Screen>();
                Assert.IsNotNull(screen);
                Assert.AreSame(context, screen.Context);
                Assert.AreSame(screen, context.Resolve<Screen>());
                Assert.IsFalse(screen.Disposed);
                using (var child = context.CreateChild())
                {
                    var screen2 = child.Resolve<Screen>();
                    Assert.IsNotNull(screen);
                    Assert.AreNotSame(screen, screen2);
                    Assert.AreSame(child, screen2.Context);
                }
            }
            Assert.IsTrue(screen.Disposed);
        }

        [TestMethod]
        public void Should_Create_Generic_Contextual_Implicitly()
        {
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor(typeof(Controller));
            HandlerDescriptor.GetDescriptor(typeof(Screen<>));
            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var screen1 = context.Provide(new Foo()).Resolve<Screen<Foo>>();
                Assert.IsNotNull(screen1);
                var screen2 = context.Infer().Resolve<Screen<Bar>>();
                 Assert.IsNotNull(screen2);
                Assert.AreSame(screen1, context.Resolve<Screen<Foo>>());
                Assert.AreSame(screen2, context.Resolve<Screen<Bar>>());
            }
        }

        [TestMethod]
        public void Should_Provide_Generic_Contextual_Implicitly()
        {
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor(typeof(Controller));
            HandlerDescriptor.GetDescriptor<ScreenProvider>();
            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var screen1 = context.Provide(new Foo()).Resolve<Screen<Foo>>();
                Assert.IsNotNull(screen1);
                var screen2 = context.Infer().Resolve<Screen<Bar>>();
                Assert.IsNotNull(screen2);
                Assert.AreSame(screen1, context.Resolve<Screen<Foo>>());
                Assert.AreSame(screen2, context.Resolve<Screen<Bar>>());
                Assert.IsNull(context.Resolve<Screen<Boo>>());
            }
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Should_Reject_Changing_Managed_Context()
        {
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor<Screen>();

            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var screen = context.Resolve<Screen>();
                Assert.AreSame(context, screen.Context);
                screen.Context = new Context();
            }
        }

        [TestMethod]
        public void Should_Detach_From_Context_If_Null()
        {
            HandlerDescriptor.ResetDescriptors();
            HandlerDescriptor.GetDescriptor<Screen>();

            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var screen = context.Resolve<Screen>();
                Assert.AreSame(context, screen.Context);
                screen.Context = null;
                Assert.AreNotSame(screen, context.Resolve<Screen>());
            }
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Should_Reject_Lifestye_If_Not_Provider()
        {
            HandlerDescriptor.GetDescriptor<SayHello>();
        }

        public class FilterResolver : Handler
        {
            public Type RequestedType { get; set; }

            [Provides]
            public object ResolveFilter(Inquiry inquiry)
            {
                RequestedType = inquiry.Key as Type;
                return null;
            }
        }

        public class RequestFilterCb<T> : IFilter<T, object>
        {
            public int? Order { get; set; }

            public Task<object> Next(T callback, MemberBinding member,
                IHandler composer, Next<object> next,
                IFilterProvider provider)
            {
                return null;
            }
        }

        public class RequestFilterRes<T> : IFilter<object, T>
        {
            public int? Order { get; set; }

            public Task<T> Next(object callback, MemberBinding member,
                IHandler composer, Next<T> next,
                IFilterProvider provider)
            {
                return Task.FromResult(default(T));
            }
        }

        public class RequestFilterBad<T> : IFilter<object, object>
        {
            public int? Order { get; set; }

            public Task<object> Next(
                object callback, MemberBinding member,
                IHandler composer, Next<object> next,
                IFilterProvider provider)
            {
                return null;
            }
        }

        internal class Callback
        {
            public List<object> Filters = new List<object>();
        }

        private class Foo : Callback
        {     
            public int  Handled     { get; set; }
            public bool HasComposer { get; set; }
        }

        private class SuperFoo : Foo
        {
        }

        private class FooDecorator : Foo
        {
            private FooDecorator(Foo foo)
            {           
            }
        }

        private class Bar : Callback
        {
            public int  Handled     { get; set; }
            public bool HasComposer { get; set; }

        }

        private class SuperBar : Bar
        {         
        }

        private class Boo : Callback
        {
            public bool HasComposer { get; set; }
        }

        private class Baz : Callback
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

        private class BazInt : Baz<int>
        {
            public BazInt(int stuff) : base(stuff)
            {              
            }
        }

        private class Bee : Callback
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
            public bool? HandleSuperFooImplict(SuperFoo foo, IHandler composer)
            {
                ++foo.Handled;
                foo.HasComposer = true;
                return null;
            }

            [Handles]
            public bool? HandleBarExplicit(Bar bar, IHandler composer)
            {
                ++bar.Handled;
                bar.HasComposer = true;
                return bar.Handled % 2 == 1 ? true : (bool?)null;
            }

            [Handles]
            public bool? HandlesGenericBaz<T>(Baz<T> baz)
            {
                if (typeof (T) == typeof (char)) 
                    return null;
                baz.Stuff = default;
                return true;
            }

            [Handles]
            public bool? HandlesGenericBazMapping<R,T>(Baz<T,R> baz)
            {
                if (typeof(T) == typeof(char))
                    return null;
                baz.Stuff      = default;
                baz.OtherStuff = default;
                return true;
            }

            [Handles]
            public static void StaticHandleFooImplict(Foo foo)
            {
                ++foo.Handled;
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
                return null;
            }

            [Provides]
            public Baz<T> ProvidesBazGenerically<T>()
            {
                return new Baz<T>(default);
            }

            [Provides]
            public Baz<T,R> ProvidesBazMapped<R,T>()
            {
                return new Baz<T,R>(default, default);
            }

            [Provides]
            public void ProvideBazExplicitly(Inquiry inquiry, IHandler composer)
            {
               if (Equals(inquiry.Key, typeof(Baz)))
                   inquiry.Resolve(new SuperBaz(), composer);
            }

            [Provides("Foo"),
             Provides("Bar")]
            public object ProvidesByName(Inquiry inquiry)
            {
                switch (inquiry.Key as string)
                {
                    case "Foo":
                        return new Foo();
                    case "Bar":
                        return new Bar();
                    default:
                        return null;
                }
            }

            [Provides("Boo", StringComparison.OrdinalIgnoreCase)]
            public object ProvidesByNameCaseInsensitive(Inquiry inquiry)
            {
                switch ((inquiry.Key as string)?.ToUpper())
                {
                    case "BOO":
                        return new Boo();
                    default:
                        return null;
                }
            }
        }

        private class CustomAsyncHandler : Handler
        {
            [Provides]
            public Promise<Bar> ProvideBarImplicitly()
            {
                return Promise.Resolved(new Bar { Handled = 1 });
            }

            [Provides]
            public Promise<Boo> ProvideBooImplicitly(IHandler composer)
            {
                return Promise.Resolved(new Boo { HasComposer = true });
            }

            [Provides]
            public Promise<SuperBar> ProvideSuperBarImplicitly(IHandler composer)
            {
                return Promise.Resolved(new SuperBar
                {
                    Handled = 1,
                    HasComposer = true
                });
            }

            [Provides]
            public Promise<Baz> ProvidesBazButIgnores()
            {
                return Promise<Baz>.Empty;
            }

            [Provides]
            public Promise<Baz<T>> ProvidesBazGenerically<T>()
            {
                return Promise.Resolved(new Baz<T>(default));
            }

            [Provides]
            public Promise<Baz<T, R>> ProvidesBazMapped<R, T>()
            {
                return Promise.Resolved(new Baz<T, R>(default, default));
            }

            [Provides]
            public void ProvideBazExplicitly(Inquiry inquiry, IHandler composer)
            {
                if (Equals(inquiry.Key, typeof(Baz)))
                    inquiry.Resolve(Promise.Resolved(new SuperBaz()), composer);
            }

            [Provides("Foo"),
             Provides("Bar")]
            public Promise ProvidesByName(Inquiry inquiry)
            {
                switch (inquiry.Key as string)
                {
                    case "Foo":
                        return Promise.Resolved(new Foo());
                    case "Bar":
                        return Promise.Resolved(new Bar());
                    default:
                        return Promise.Empty;
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
            public object ProvideBooKey(IHandler composer, PolicyMemberBinding binding)
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

            [Provides]
            public Bar ProvideBar { get; } = new Bar { Handled = 3 };

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
            public object ProvideManyKeys(Inquiry inquiry)
            {
                if (Equals(inquiry.Key, typeof(Baz<int>)))
                    return new Baz<int>(1);
                if (Equals(inquiry.Key, typeof(Baz<string>)))
                    return new Baz<string>("Hello");
                return null;
            }

            [Provides]
            public void ProvideBazExplicitly(Inquiry inquiry, IHandler composer,
                                             PolicyMemberBinding binding)
            {
                if (Equals(inquiry.Key, typeof(Baz)))
                {
                    inquiry.Resolve(new SuperBaz(), composer);
                    inquiry.Resolve(new Baz(), composer);
                }
            }

            [Provides]
            public TFoo ProvidesNewFoo<TFoo>()
                where TFoo : Foo, new()
            {
                return new TFoo();
            }
        }

        private class SpecialAsyncHandler : Handler
        {
            [Provides(typeof(Boo))]
            public Promise ProvideBooKey(IHandler composer, PolicyMemberBinding binding)
            {
                return Promise.Resolved(new Boo { HasComposer = true });
            }

            [Provides]
            public Task<Bar[]> ProvideManyBar()
            {
                return Task.FromResult(new[]
                {
                    new Bar {Handled = 1},
                    new Bar {Handled = 2}
                });
            }

            [Provides]
            public Promise<Bar> ProvideBar { get; } 
                = Promise.Resolved(new Bar { Handled = 3 });

            [Provides(typeof(Bee))]
            public Promise ProvideManyBeeWithKey()
            {
                return Promise.Resolved(new[]
                {
                    new Bee(), new Bee(), new Bee()
                });
            }

            [Provides(typeof(Baz<int>)),
             Provides(typeof(Baz<string>))]
            public Task ProvideManyKeys(Inquiry inquiry)
            {
                if (Equals(inquiry.Key, typeof(Baz<int>)))
                    return Promise.Resolved(new Baz<int>(1));
                if (Equals(inquiry.Key, typeof(Baz<string>)))
                    return Promise.Resolved(new Baz<string>("Hello"));
                return Promise.Empty;
            }

            [Provides]
            public void ProvideBazExplicitly(Inquiry inquiry, IHandler composer,
                                             PolicyMemberBinding binding)
            {
                if (Equals(inquiry.Key, typeof(Baz)))
                {
                    inquiry.Resolve(Promise.Resolved(new SuperBaz()), composer);
                    inquiry.Resolve(Promise.Resolved(new Baz()), composer);
                }
            }
        }

        private class ArrayHandler : Handler
        {
            [Handles]
            public string HandlesIntegers(int[] array)
            {
                return "integers";
            }

            [Handles]
            public string HandlesStrings(string[] array)
            {
                return "string";
            }

            [Handles]
            public string HandlesTypes(Type[] array)
            {
                return "types";
            }

            [Handles]
            public string HandlesArray(Array array)
            {
                return "array";
            }

            public string HandlesError(object instance)
            {
                return "error";
            }

            [Provides(Strict = true)]
            public int[] ProvidesIntegers()
            {
                return new[] {2, 4, 6};
            }

            [Provides(Strict = true)]
            public string[] ProvidesStrings()
            {
                return new[] { "square", "circle" };
            }

            [Provides(Strict = true)]
            public Type[] ProvidesTypes()
            {
                return new[] { typeof(float), typeof(object) };
            }
        }

        private class BadHandler : Handler
        {
            [Handles]
            public int Add()
            {
                return 22;
            }
        }

        private class BadProvider : Handler
        {
            [Provides]
            public void Add(int num1, int num2)
            {
            }
        }

        private class Controller
        {
            [Provides]
            public Controller()
            {            
            }

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

        private class Controller<TView, TModel> : Controller
        {
            [Provides]
            public Controller(TView view, TModel model)
            {
                View  = view;
                Model = model;
            }

            public TView  View  { get; }
            public TModel Model { get; }
        }

        private class Screen : Contextual, IDisposable
        {
            [Provides, Contextual]
            public Screen()
            {
            }

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private class Screen<TModel> : Screen
        {
            [Provides, Contextual]
            public Screen(TModel model)
            {
                Model = model;
            }

            public TModel Model { get; }
        }

        private class ScreenProvider
        {
            [Provides, Contextual]
            public static Promise<Screen<TModel>> GetScreen<TModel>(TModel model)
            {
                return Promise.Resolved(new Screen<TModel>(model));
            }
        }

        private class Application
        {
            [Provides, Singleton]
            public Application()
            {
                
            }
        }

        private class Application<C>
            where C : Controller
        {
            [Provides, Singleton]
            public Application(C rootController, Screen screen)
            {
                RootController = rootController;
                MainScreen     = screen;
            }

            public C      RootController { get; }
            public Screen MainScreen     { get; }
        }

        private class SayHello
        {
            [Handles, Singleton]
            public string Hello(string name) => $"Hello {name}";
        }

        private class FilteredHandler : Handler, IFilter<Bar, object>
        {
            int? IFilter.Order { get; set; }

            [Handles,
             Filter(typeof(IFilter<,>),
                 Many = true, Required = true)]
            public void HandleBar(Bar bar)
            {
                bar.Handled++;
            }

            [Handles,
             Filter(typeof(IFilter<,>), Many = true),
             SkipFilters]
            public void HandleBee(Bee bee)
            {
            }

            [Provides(typeof(IFilter<,>))]
            public object CreateFilter(Inquiry inquiry)
            {
                var type = (Type)inquiry.Key;
                if (type.IsGenericTypeDefinition) return null;
                if (type.IsInterface)
                    return Activator.CreateInstance(
                        typeof(LogFilter<,>).
                        MakeGenericType(type.GenericTypeArguments));
                return type.IsAbstract ? null
                     : Activator.CreateInstance(type);
            }

            [Provides(typeof(ExceptionBehavior<,>))]
            public object CreateExceptionBehaviorT(Inquiry inquiry)
            {
                var type = (Type)inquiry.Key;
                if (type.IsGenericTypeDefinition) return null;
                if (type.IsInterface)
                    return Activator.CreateInstance(
                        typeof(ExceptionBehavior<,>).
                        MakeGenericType(type.GenericTypeArguments));
                return type.IsAbstract ? null
                     : Activator.CreateInstance(type);
            }

            Task<object> IFilter<Bar, object>.Next(
                Bar callback, MemberBinding binding, IHandler composer,
                Next<object> next, IFilterProvider provider)
            {
                callback.Filters.Add(this);
                callback.Handled++;
                return next();
            }
        }

        private class SpecialFilteredHandler : Handler
        {
            [Handles,
             Filter(typeof(IFilter<,>), Many = true)]
            public SuperFoo HandleFoo(Foo foo)
            {
                return new SuperFoo();
            }

            [Handles,
             Filter(typeof(IFilter<,>), Many = true)]
            public Promise<SuperBaz> HandleBaz(Baz baz)
            {
                return Promise.Resolved(new SuperBaz());
            }

            [Handles,
             Filter(typeof(IFilter<,>), Many = true)]
            public Task<SuperBar> HandleBaz(Bar bar)
            {
                return Task.FromResult(new SuperBar());
            }

            [Handles,
             Filter(typeof(ExceptionBehavior<,>))]
            public void Remove(Boo boo)
            {          
            }
        }

        internal static Callback ExtractCallback(object callback)
        {
            var cb = callback as Callback;
            if (cb == null)
            {
                if (callback is Command command)
                    cb = command.Callback as Callback;
            }
            return cb;
        }

        private class LogFilter<Cb, Res> : IFilter<Cb, Res>
        {
            public int? Order { get; set; } = 1;

            public Task<Res> Next(Cb callback, MemberBinding binding,
                IHandler composer, Next<Res> next,
                IFilterProvider provider)
            {
                var cb = ExtractCallback(callback);
                cb?.Filters.Add(this);
                Console.WriteLine($@"Filter log {callback}");
                return next();
            }
        }

        private class ExceptionBehavior<Req, Res> : IFilter<Req, Res>
        {
            public int? Order { get; set; } = 2;

            public Task<Res> Next(Req request, MemberBinding binding,
                IHandler composer, Next<Res> next,
                IFilterProvider provider)
            {
                return Promise<Res>.Rejected(
                    new InvalidOperationException("System shutdown"));
            }
        }
    }
}
