namespace Miruken.Tests.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Callback.Policy.Bindings;
    using Miruken.Concurrency;
    using Miruken.Context;
    using Miruken.Infrastructure;

    [TestClass]
    public class HandlerTests
    {
        private IHandlerDescriptorFactory _factory;

        [TestInitialize]
        public void TestInitialize()
        {
            _factory = new MutableHandlerDescriptorFactory();
            _factory.RegisterDescriptor<CustomHandler>();
            _factory.RegisterDescriptor<CustomAsyncHandler>();
            _factory.RegisterDescriptor<SpecialHandler>();
            _factory.RegisterDescriptor<SpecialAsyncHandler>();
            _factory.RegisterDescriptor<ArrayHandler>();
            _factory.RegisterDescriptor<CallbackContextHandler>();
            _factory.RegisterDescriptor<FilteredHandler>();
            _factory.RegisterDescriptor<SpecialFilteredHandler>();
            _factory.RegisterDescriptor<FilterHandlerTests>();
            _factory.RegisterDescriptor<Controller>();
            _factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(_factory);
        }

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
        public void Should_Handle_Callbacks_With_CallbackContext()
        {
            var foo     = new Foo();
            var handler = new CallbackContextHandler();
            Assert.IsTrue(handler.Handle(foo));
            Assert.AreEqual(1, foo.Handled);
            Assert.IsTrue(foo.HasComposer);
        }

        [TestMethod]
        public void Should_Mark_Callback_Not_Handled()
        {
            var handler = new CallbackContextHandler();
            Assert.IsFalse(handler.Handle(new Bar()));
        }

        [TestMethod]
        public void Should_Mark_Callback_With_Return_Not_Handled()
        {
            var handler = new CallbackContextHandler();
            Assert.IsTrue(handler.Handle(new Baz<int>(4)));
            Assert.IsFalse(handler.Handle(new Baz<int>(22)));
        }

        [TestMethod]
        public void Should_Mark_Promise_Callback_Not_Handled()
        {
            var handler = new CallbackContextHandler();
            Assert.IsFalse(handler.Handle(new Baz()));
        }

        [TestMethod]
        public void Should_Mark_Task_Callback_Not_Handled()
        {
            var handler = new CallbackContextHandler();
            Assert.IsFalse(handler.Handle(new Boo()));
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Inferred_Greedy()
        {
            var foo     = new Foo();
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Infer().Handle(foo, true));
            Assert.AreEqual(1, foo.Handled);
        }

        [TestMethod]
        public void Should_Handle_Static_Callbacks_Implicitly()
        {
           _factory.RegisterDescriptor<CustomHandler>();
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
            var foo = new SpecialFoo();
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(foo));
            Assert.IsTrue(foo.HasComposer);
            Assert.AreEqual(2, foo.Handled);
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Generically()
        {
            var baz = new Baz<int>(22);
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(baz));
            Assert.AreEqual(0, baz.Stuff);
            Assert.IsFalse(handler.Handle(new Baz<char>('M')));
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Generically_Mapped()
        {
            var baz = new Baz<int, float>(22, 15.5f);
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(baz));
            Assert.AreEqual(0, baz.Stuff);
            Assert.AreEqual(0, baz.OtherStuff);
            Assert.IsFalse(handler.Handle(new Baz<char, float>('M', 2)));
        }

        [TestMethod]
        public void Should_Handle_Callbacks_Implicitly_Generically()
        {
            var baz = new BazInt(29);
            var handler = new CustomHandler();
            Assert.IsTrue(handler.Handle(baz));
            Assert.AreEqual(0, baz.Stuff);
        }

        [TestMethod]
        public void Should_Replace_Composer_In_Filter()
        {
            var handler = new FilterHandlerTests();
            var bar = handler.Command<Bar>(new Foo());
            Assert.IsNotNull(bar);
        }

        [TestMethod]
        public void Should_Handle_Arrays()
        {
            var handler = new ArrayHandler();
            var type = handler.Command<string>(new[] { 1, 2, 3 });
            Assert.AreEqual("integers", type);
            type = handler.Command<string>(new[] { "red", "green", "blue" });
            Assert.AreEqual("string", type);
            type = handler.Command<string>(new[] { typeof(int), typeof(string) });
            Assert.AreEqual("types", type);
            type = handler.Command<string>(new[] { 'a', 'b', 'c' });
            Assert.AreEqual("array", type);
        }

        [TestMethod]
        public void Should_Provide_Arrays()
        {
            var handler = new ArrayHandler();
            Array array = handler.Resolve<int[]>();
            CollectionAssert.AreEqual(new[] { 2, 4, 6 }, array);
            array = handler.Resolve<string[]>();
            CollectionAssert.AreEqual(new[] { "square", "circle" }, array);
            array = handler.Resolve<Type[]>();
            CollectionAssert.AreEqual(new[] { typeof(float), typeof(object) }, array);
        }

        [TestMethod]
        public void Should_Handle_Callbacks_With_Keys()
        {
            var foo = new Foo();
            var handler = new SpecialHandler();
            Assert.IsTrue(handler.Handle(foo, true));
            Assert.AreEqual(1, foo.Handled);
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Should_Reject_Handlers()
        {
            _factory.RegisterDescriptor<BadHandler>();
        }

        [TestMethod]
        public void Should_Indicate_Not_Provided()
        {
            var handler = new CustomHandler();
            var bee = handler.Resolve<Bee>();
            Assert.IsNull(bee);
        }

        [TestMethod]
        public void Should_Indicate_Not_Provided__Adapter()
        {
            var handler = new HandlerAdapter(new Controller());
            var bee = handler.Resolve<Bee>();
            Assert.IsNull(bee);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Implicitly()
        {
            var handler = new CustomHandler();
            var bar = handler.Resolve<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public async Task Should_Provide_Callbacks_Implicitly_Async()
        {
            var handler = new CustomAsyncHandler();
            var bar = await handler.ResolveAsync<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Implicitly_Waiting()
        {
            var handler = new CustomAsyncHandler();
            var bar = handler.Resolve<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Provide_Many_Callbacks_Implicitly()
        {
            var handler = new SpecialHandler();
            var bar = handler.Resolve<Bar>();
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
        public async Task Should_Provide_Many_Callbacks_Implicitly_Async()
        {
            var handler = new SpecialAsyncHandler();
            var bar = await handler.ResolveAsync<Bar>();
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
            var boo = handler.Resolve<Boo>();
            Assert.IsNotNull(boo);
            Assert.IsTrue(boo.HasComposer);
        }

        [TestMethod]
        public async Task Should_Provide_Callbacks_By_Key_Async()
        {
            var handler = new SpecialAsyncHandler();
            var boo = await handler.ResolveAsync<Boo>();
            Assert.IsNotNull(boo);
            Assert.IsTrue(boo.HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Many_Callbacks_By_Key()
        {
            var handler = new SpecialHandler();
            var bees = handler.ResolveAll<Bee>();
            Assert.AreEqual(3, bees.Length);
        }

        [TestMethod]
        public async Task Should_Provide_Many_Callbacks_By_Key_Async()
        {
            var handler = new SpecialAsyncHandler();
            var bees = await handler.ResolveAllAsync<Bee>();
            Assert.AreEqual(3, bees.Length);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_With_Many_Keys()
        {
            var handler = new SpecialHandler();
            var baz1 = handler.Resolve<Baz<int>>();
            Assert.AreEqual(1, baz1.Stuff);
            var baz2 = handler.Resolve<Baz<string>>();
            Assert.AreEqual("Hello", baz2.Stuff);
            var baz3 = handler.Resolve<Baz<float>>();
            Assert.IsNull(baz3);
        }

        [TestMethod]
        public async Task Should_Provide_Callbacks_With_Many_Keys_Async()
        {
            var handler = new SpecialAsyncHandler();
            var baz1 = await handler.ResolveAsync<Baz<int>>();
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
            var bar = handler.Resolve<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Implicitly_With_Composer()
        {
            var handler = new CustomHandler();
            var boo = handler.Resolve<Boo>();
            Assert.IsNotNull(boo);
            Assert.AreEqual(boo.GetType(), typeof(Boo));
            Assert.IsTrue(boo.HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Covariantly()
        {
            var handler = new CustomHandler();
            var bar = handler.Resolve<SpecialBar>();
            Assert.IsInstanceOfType(bar, typeof(SpecialBar));
            Assert.IsTrue(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Greedily()
        {
            var handler = new CustomHandler() + new CustomHandler();
            var bars = handler.ResolveAll<Bar>();
            Assert.AreEqual(4, bars.Length);
            bars = handler.ResolveAll<SpecialBar>();
            Assert.AreEqual(2, bars.Length);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Explicitly()
        {
            var handler = new CustomHandler();
            var baz = handler.Resolve<Baz>();
            Assert.IsInstanceOfType(baz, typeof(SpecialBaz));
            Assert.IsFalse(baz.HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Many_Callbacks_Explicitly()
        {
            var handler = new SpecialHandler();
            var baz = handler.Resolve<Baz>();
            Assert.IsInstanceOfType(baz, typeof(SpecialBaz));
            Assert.IsFalse(baz.HasComposer);
            var bazs = handler.ResolveAll<Baz>();
            Assert.AreEqual(2, bazs.Length);
            Assert.IsInstanceOfType(bazs[0], typeof(SpecialBaz));
            Assert.IsFalse(bazs[0].HasComposer);
            Assert.IsInstanceOfType(bazs[1], typeof(Baz));
            Assert.IsFalse(bazs[1].HasComposer);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Generically()
        {
            var handler = new CustomHandler();
            var baz = handler.Resolve<Baz<int>>();
            Assert.IsInstanceOfType(baz, typeof(Baz<int>));
            Assert.AreEqual(0, baz.Stuff);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_Mapped()
        {
            var handler = new CustomHandler();
            var baz = handler.Resolve<Baz<int, string>>();
            Assert.IsInstanceOfType(baz, typeof(Baz<int, string>));
            Assert.AreEqual(0, baz.Stuff);
        }

        [TestMethod]
        public void Should_Provide_All_Callbacks()
        {
            var handler = new CustomHandler();
            var bars = handler.ResolveAll<Bar>();
            Assert.AreEqual(2, bars.Length);
        }

        [TestMethod]
        public void Should_Provide_Empty_Array_If_No_Matches()
        {
            var handler = new Handler();
            var bars = handler.ResolveAll<Bar>();
            Assert.AreEqual(0, bars.Length);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_By_String_Key()
        {
            var handler = new CustomHandler();
            var bar = handler.Resolve("Bar") as Bar;
            Assert.IsNotNull(bar);
        }

        [TestMethod]
        public void Should_Provide_Callbacks_By_String_Case_Insensitive_Key()
        {
            var handler = new CustomHandler();
            var boo = handler.Resolve("boo") as Boo;
            Assert.IsNotNull(boo);
        }

        [TestMethod]
        public void Should_Provide_Callback_Using_Constraints()
        {
            var handler = new SpecialHandler();
            Assert.IsNotNull(handler.Resolve<Foo>());
            Assert.IsNotNull(handler.Resolve<SpecialFoo>());
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
            _factory.RegisterDescriptor<BadProvider>();
        }

        [TestMethod]
        public async Task Should_Filter_Async_Resolution()
        {
            var handler = new CustomHandler();
            var bar = await handler.Aspect((_, c) => true).ResolveAsync<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Async_Filter_Resolution()
        {
            var handler = new CustomHandler();
            var bar = handler.Aspect((_, c) => Promise.True).Resolve<Bar>();
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public async Task Should_Async_Filter_Async_Resolution()
        {
            var handler = new CustomHandler();
            var bar = await handler.Aspect((_, c) => Promise.True).ResolveAsync<Bar>();
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
            var result = handler.Resolve<CustomHandler>();
            Assert.AreSame(handler, result);
        }

        [TestMethod]
        public void Should_Resolve_Self_Implicitly_Decorated()
        {
            var handler = new CustomHandler();
            var result = handler.Broadcast().Resolve<CustomHandler>();
            Assert.AreSame(handler, result);
        }

        [TestMethod]
        public void Should_Resolve_Self_Adapter_Implicitly()
        {
            var controller = new Controller();
            var handler = new HandlerAdapter(controller);
            var result = handler.Resolve<Controller>();
            Assert.AreSame(controller, result);
        }

        [TestMethod]
        public void Should_Resolve_Self_Adapter_Implicitly_Decorated()
        {
            var controller = new Controller();
            var handler = new HandlerAdapter(controller);
            var result = handler.Broadcast().Resolve<Controller>();
            Assert.AreSame(controller, result);
        }

        [TestMethod]
        public void Should_Resolve_Using_IServiceProvider()
        {
            var handler = (IServiceProvider)new CustomHandler();
            var bar = (Bar)handler.GetService(typeof(Bar));
            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.HasComposer);
            Assert.AreEqual(1, bar.Handled);
        }

        [TestMethod]
        public void Should_Resolve_All()
        {
            var custom = new CustomHandler();
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
            var foo = new Foo();
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
            var foo = handler.Provide(new Foo()).Resolve<Foo>();
            Assert.IsNotNull(foo);
        }

        [TestMethod]
        public void Should_Override_Providers_Many()
        {
            var foo1 = new Foo();
            var foo2 = new Foo();
            var handler = new Handler();
            var foos = handler.Provide(new[] { foo1, foo2 }).ResolveAll<Foo>();
            CollectionAssert.AreEqual(new[] { foo1, foo2 }, foos);
        }

        [TestMethod]
        public void Should_Ignore_Providers()
        {
            var handler = new Handler();
            var foo = handler.Provide(new Bar()).Resolve<Foo>();
            Assert.IsNull(foo);
        }

        [TestMethod]
        public void Should_Create_Filters()
        {
            var bar     = new Bar();
            var handler = new FilteredHandler();
            Assert.IsTrue(handler.Handle(bar));
            Assert.AreEqual(4, bar.Filters.Count);
            Assert.IsTrue(bar.Filters.Contains(handler));
            Assert.IsTrue(bar.Filters.OfType<ContravariantFilter>().Count() == 1);
            Assert.IsTrue(bar.Filters.OfType<LogFilter<Bar, object>>().Count() == 1);
            Assert.IsTrue(bar.Filters.OfType<ExceptionBehavior<Bar, object>>().Count() == 1);
        }

        [TestMethod]
        public void Should_Skip_Filters()
        {
            var bee = new Bee();
            var handler = new FilteredHandler();
            Assert.IsTrue(handler.Handle(bee));
            Assert.AreEqual(0, bee.Filters.Count);
        }

        [TestMethod]
        public void Should_Skip_Non_Required_Filters()
        {
            var bar     = new Bar();
            var handler = new FilteredHandler();
            Assert.IsTrue(handler.SkipFilters().Handle(bar));
            Assert.AreEqual(3, bar.Filters.Count);
            Assert.IsTrue(bar.Filters.Contains(handler));
            Assert.IsTrue(bar.Filters.OfType<ContravariantFilter>().Count() == 1);
            Assert.IsTrue(bar.Filters.OfType<ExceptionBehavior<Bar, object>>().Count() == 1);
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public async Task Should_Propagate_Rejected_Filter_Promise()
        {
            var boo     = new Boo();
            var handler = new SpecialFilteredHandler() + new FilteredHandler();
            await handler.CommandAsync(boo);
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
            _factory.RegisterDescriptor<Controller>();
            var instance = handler.Resolve<Controller>();
            Assert.IsNotNull(instance);
            Assert.AreNotSame(instance, handler.Resolve<Controller>());
        }

        [TestMethod]
        public void Should_Create_Generic_Implicitly()
        {
            var view = new Screen();
            var bar  = new SpecialBar();
            _factory.RegisterDescriptor(typeof(Controller<,>));
            _factory.RegisterDescriptor<Provider>(); 
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
            var foo = new Foo();
            _factory.RegisterDescriptor<Controller>();
            Assert.IsTrue(new StaticHandler().Infer().Handle(foo));
            Assert.AreEqual(1, foo.Handled);
        }

        [TestMethod]
        public void Should_Create_Resolving_Generic_Implicitly()
        {
            var boo = new Boo();
            var baz = new SpecialBaz();
            _factory.RegisterDescriptor<Controller>();
            _factory.RegisterDescriptor(typeof(Controller<,>));
            _factory.RegisterDescriptor<Provider>();
            var instance = new StaticHandler().Infer()
                .Provide(boo).Provide(baz)
                .Resolve<Controller<Boo, Baz>>();
            Assert.IsNotNull(instance);
            Assert.AreSame(boo, instance.View);
            Assert.AreSame(baz, instance.Model);
        }

        [TestMethod]
        public void Should_Provide_Instance_Implicitly()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<Controller>();
            var bar = new StaticHandler().Infer().Resolve<Bar>();
            Assert.IsNotNull(bar);
        }

        [TestMethod]
        public void Should_Provide_Dependencies_Implicitly()
        {
            var view = new Screen();
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<Controller>();
            factory.RegisterDescriptor(typeof(Controller<,>));
            factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(factory);
            var instance = new StaticHandler().Infer()
                .Provide(view).Resolve<Controller<Screen, Bar>>();
            Assert.IsNotNull(instance);
            Assert.AreSame(view, instance.View);
        }

        [TestMethod]
        public void Should_Detect_Circular_Dependencies()
        {
            var view    = new Screen();
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<Provider>();
            factory.RegisterDescriptor(typeof(Controller<,>));
            HandlerDescriptorFactory.UseFactory(factory);
            var instance = new StaticHandler().Infer()
                .Provide(view).Resolve<Controller<Screen, Bar>>();
            Assert.IsNull(instance);
        }

        [TestMethod]
        public void Should_Create_Singletons_Implicitly()
        {
            var handler = new StaticHandler();
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<Application>();
            var app = handler.Resolve<Application>();
            Assert.IsNotNull(app);
            Assert.AreSame(app, handler.Resolve<Application>());
        }

        [TestMethod]
        public async Task Should_Initialize_Asynchronously()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor(typeof(InitializedComponent1));
            var component = await new StaticHandler().ResolveAsync<InitializedComponent1>();
            Assert.IsNotNull(component);
            Assert.IsTrue(component.Initialized);
        }

        [TestMethod]
        public void Should_Create_Generic_Singletons_Implicitly()
        {
            var view    = new Screen();
            var handler = new StaticHandler().Infer();
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<Controller>();
            factory.RegisterDescriptor(typeof(Controller<,>));
            factory.RegisterDescriptor(typeof(Application<>));
            factory.RegisterDescriptor(typeof(InitializedComponent1));
            factory.RegisterDescriptor(typeof(InitializedComponent2));
            factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(factory);
            var app1 = handler.Provide(view)
                .Resolve<Application<Controller<Screen, Bar>>>();
            Assert.IsNotNull(app1);
            Assert.IsTrue(app1.Initialized);
            Assert.AreEqual(1, app1.InitializedCount);
            Assert.AreSame(view, app1.RootController.View);
            Assert.AreSame(view, app1.MainScreen);
            var app2 = handler.Provide(view)
                .Resolve<Application<Controller<Screen, Bar>>>();
            Assert.AreSame(app1, app2);
            Assert.IsTrue(app2.Initialized);
            Assert.AreEqual(1, app2.InitializedCount);
            var app3 = handler.Provide(view)
                .Resolve<IApplication<Controller<Screen, Bar>>>();
            Assert.AreSame(app1, app3);
        }

        [TestMethod]
        public void Should_Return_Same_Contextual_Without_Qualifier()
        {
            Screen screen;
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<Screen>();
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
                    Assert.AreSame(screen, screen2);
                    Assert.AreSame(child.Parent, screen2.Context);
                }
            }
            Assert.IsTrue(screen.Disposed);
        }

        [TestMethod]
        public void Should_Create_Contextual_Implicitly()
        {
            Screen screen;
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<Screen>();
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
                    var screen2 = child.Resolve<Screen>(constraints =>
                        constraints.Require(Qualifier.Of<ContextualAttribute>()));
                    Assert.IsNotNull(screen2);
                    Assert.AreNotSame(screen, screen2);
                    Assert.AreSame(child, screen2.Context);
                }
            }
            Assert.IsTrue(screen.Disposed);
        }

        [TestMethod]
        public void Should_Create_Rooted_Contextual_Implicitly()
        {
            RootedComponent rooted;
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<RootedComponent>();
            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                rooted = context.Resolve<RootedComponent>();
                Assert.IsNotNull(rooted);
                using (var child = context.CreateChild())
                {
                    var rooted2 = child.Resolve<RootedComponent>(constraints =>
                        constraints.Require(Qualifier.Of<ContextualAttribute>()));
                    Assert.IsNotNull(rooted2);
                    Assert.AreSame(rooted, rooted2);
                }
            }
            Assert.IsTrue(rooted.Disposed);
        }

        [TestMethod]
        public void Should_Create_Contextual_Covariantly()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor(typeof(Screen<>));
            factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(factory);
            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var view = context.Provide(new Bar()).Resolve<IView<Bar>>();
                Assert.IsNotNull(view);
                Assert.AreSame(view, context.Resolve<IView<Bar>>());
            }
        }

        [TestMethod]
        public void Should_Create_Contextual_Covariantly_Inferred()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor(typeof(Controller));
            factory.RegisterDescriptor(typeof(Screen<>));
            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var view = context.Infer().Resolve<IView<Bar>>();
                Assert.IsNotNull(view);
                Assert.AreSame(view, context.Resolve<IView<Bar>>());
            }
        }

        [TestMethod]
        public void Should_Create_Generic_Contextual_Implicitly()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor(typeof(Controller));
            factory.RegisterDescriptor(typeof(Screen<>));
            factory.RegisterDescriptor<Provider>();
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
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<Controller>();
            factory.RegisterDescriptor<ScreenProvider>();
            factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(factory);
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

        [TestMethod]
        public void Should_Reject_Contextual_Creation_If_No_Context()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<Screen>();
            var screen = new StaticHandler().Resolve<Screen>();
            Assert.IsNull(screen);
        }

        [TestMethod,
         ExpectedException(typeof(InvalidOperationException))]
        public void Should_Reject_Changing_Managed_Context()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<Screen>();

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
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<Screen>();

            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var screen = context.Resolve<Screen>();
                Assert.AreSame(context, screen.Context);
                screen.Context = null;
                Assert.AreNotSame(screen, context.Resolve<Screen>());
                Assert.IsTrue(screen.Disposed);
            }
        }

        [TestMethod]
        public async Task Should_Reject_Constructor_If_Initializer_Fails()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<FailedInitialization>();
            var result = await (new StaticHandler()).ResolveAsync<FailedInitialization>();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Should_Reject_Contextual_In_Singleton()
        {
            var factory = new MutableHandlerDescriptorFactory();
            HandlerDescriptorFactory.UseFactory(factory);
            factory.RegisterDescriptor<Screen>();
            factory.RegisterDescriptor<LifestyleMismatch>();
            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var c = context.Resolve<LifestyleMismatch>();
                Assert.IsNull(c);
            }
        }

        [TestMethod]
        public void Should_Select_Greediest_Constructor()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<OverloadedConstructors>();
            factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(factory);

            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var ctor = context.Resolve<OverloadedConstructors>();
                Assert.IsNotNull(ctor);
                Assert.IsNull(ctor.Foo);
                Assert.IsNull(ctor.Bar);
                using (var child = context.CreateChild())
                {
                    ctor = child.Provide(new Foo()).Resolve<OverloadedConstructors>();
                    Assert.IsNotNull(ctor);
                    Assert.IsNotNull(ctor.Foo);
                    Assert.IsNull(ctor.Bar);
                }
                using (var child = context.CreateChild())
                {
                    ctor = child.Provide(new Foo()).Provide(new Bar())
                        .Resolve<OverloadedConstructors>();
                    Assert.IsNotNull(ctor);
                    Assert.IsNotNull(ctor.Foo);
                    Assert.IsNotNull(ctor.Bar);
                }
            }
        }

        [TestMethod]
        public void Should_Select_Greediest_Provider()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<OverloadedProviders>();
            factory.RegisterDescriptor<Provider>();
            HandlerDescriptorFactory.UseFactory(factory);

            using (var context = new Context())
            {
                context.AddHandlers(new StaticHandler());
                var provider = context.Resolve<OverloadedProviders>();
                Assert.IsNotNull(provider);
                Assert.IsNull(provider.Foo);
                Assert.IsNull(provider.Bar);
                using (var child = context.CreateChild())
                {
                    provider = child.Provide(new Foo()).Resolve<OverloadedProviders>();
                    Assert.IsNotNull(provider);
                    Assert.IsNotNull(provider.Foo);
                    Assert.IsNull(provider.Bar);
                }
                using (var child = context.CreateChild())
                {
                    provider = child.Provide(new Foo()).Provide(new Bar())
                        .Resolve<OverloadedProviders>();
                    Assert.IsNotNull(provider);
                    Assert.IsNotNull(provider.Foo);
                    Assert.IsNotNull(provider.Bar);
                }
            }
        }

        public class RequestFilterCb<T> : IFilter<T, object>
        {
            public int? Order { get; set; }

            public Task<object> Next(T callback,
                object rawCallback, MemberBinding member,
                IHandler composer, Next<object> next,
                IFilterProvider provider)
            {
                return null;
            }
        }

        public class RequestFilterRes<T> : IFilter<object, T>
        {
            public int? Order { get; set; }

            public Task<T> Next(object callback,
                object rawCallback, MemberBinding member,
                IHandler composer, Next<T> next,
                IFilterProvider provider)
            {
                return Task.FromResult(default(T));
            }
        }

        public class RequestFilterBad<T> : IFilter<object, object>
        {
            public int? Order { get; set; }

            public Task<object> Next(object callback,
                object rawCallback, MemberBinding member,
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

        private class SpecialFoo : Foo
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
            public int Handled { get; set; }
            public bool HasComposer { get; set; }

        }

        private class SpecialBar : Bar
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

        private class SpecialBaz : Baz
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

        private class Baz<T, R> : Baz<T>
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
            public void HandleFooImplicit(Foo foo)
            {
                ++foo.Handled;
            }

            [Handles]
            public bool? HandleSpecialFooImplicit(SpecialFoo foo, IHandler composer)
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
                if (typeof(T) == typeof(char))
                    return null;
                baz.Stuff = default;
                return true;
            }

            [Handles]
            public bool? HandlesGenericBazMapping<R, T>(Baz<T, R> baz)
            {
                if (typeof(T) == typeof(char))
                    return null;
                baz.Stuff = default;
                baz.OtherStuff = default;
                return true;
            }

            [Handles]
            public static void StaticHandleFooImplicit(Foo foo)
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
            public SpecialBar ProvideSpecialBarImplicitly(IHandler composer)
            {
                return new SpecialBar
                {
                    Handled = 1,
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
            public Baz<T, R> ProvidesBazMapped<R, T>()
            {
                return new Baz<T, R>(default, default);
            }

            [Provides]
            public void ProvideBazExplicitly(Inquiry inquiry, IHandler composer)
            {
                if (Equals(inquiry.Key, typeof(Baz)))
                    inquiry.Resolve(new SpecialBaz(), composer);
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
            public Promise<SpecialBar> ProvideSpecialBarImplicitly(IHandler composer)
            {
                return Promise.Resolved(new SpecialBar
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
                    inquiry.Resolve(Promise.Resolved(new SpecialBaz()), composer);
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
                    inquiry.Resolve(new SpecialBaz(), composer);
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
                    inquiry.Resolve(Promise.Resolved(new SpecialBaz()), composer);
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
                return new[] { 2, 4, 6 };
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

        private class CallbackContextHandler : Handler
        {
            [Handles]
            public void HandleFoo(Foo foo, CallbackContext context)
            {
                ++foo.Handled;
                foo.HasComposer = context.Composer != null;
            }

            [Handles]
            public void HandleBar(Bar bar, CallbackContext context)
            {
                context.NotHandled();
            }

            [Handles]
            public Promise HandleBaz(Baz baz, CallbackContext context)
            {
                context.NotHandled();
                return Promise.Delay(10.Millis());
            }

            [Handles]
            public async Task HandleBoo(Boo boo, CallbackContext context)
            {
                context.NotHandled();
                await Task.Delay(10);
            }

            [Handles]
            public async Task<int> HandleBaz(Baz<int> baz, CallbackContext context)
            {
                if (baz.Stuff > 10)
                    return context.NotHandled<int>();
                await Task.Delay(10);
                return baz.Stuff;
            }
        }

        private class FilterHandlerTests : Handler
        {
            [Handles,
             Filter(typeof(ReplaceComposerFilter<,>))]
            public Bar HandleFoo(Foo foo, Bar bar)
            {
                return bar;
            }

            [Provides(typeof(ReplaceComposerFilter<,>))]
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
            public void HandleFooImplicit(Foo foo)
            {
                ++foo.Handled;
            }

            [Provides]
            public Bar ProvideBarImplicitly()
            {
                return new Bar { Handled = 1 };
            }
        }

        private interface IView<out TModel>
        {
            TModel Model { get; }
        }

        private class Controller<TView, TModel> : Controller
        {
            [Provides]
            public Controller(TView view, TModel model)
            {
                View = view;
                Model = model;
            }

            public TView View { get; }
            public TModel Model { get; }
        }

        private class Screen : Contextual, IDisposable
        {
            [Contextual]
            public Screen()
            {
            }

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private class Screen<TModel> : Screen, IView<TModel>
        {
            [Contextual]
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
            protected Application()
            {
            }
        }

        private interface IApplication<out C>
            where C : Controller
        {
            C      RootController { get; }
            Screen MainScreen     { get; }
        }

        private class Application<C> : Application, IApplication<C>, IInitialize
            where C : Controller
        {
            [Singleton]
            public Application(C rootController, Screen screen,
                InitializedComponent1 component1, InitializedComponent2 component2)
            {
                RootController = rootController;
                MainScreen = screen;
            }

            public C      RootController   { get; }
            public Screen MainScreen       { get; }
            public bool   Initialized      { get; set; }
            public int    InitializedCount { get; private set; }

            public Promise Initialize()
            {
                return Promise.True.Then((res, _) =>
                {
                    Initialized = true;
                    ++InitializedCount;
                });
            }

            public void FailedInitialize(Exception exception = null)
            {
            }
        }

        private class RootedComponent : IDisposable
        {
            [Contextual(Rooted = true)]
            public RootedComponent()
            {
                
            }

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private class InitializedComponent1 : IInitialize
        {
            [Singleton]
            public InitializedComponent1()
            {             
            }

            public bool Initialized { get; set; }

            public Promise Initialize()
            {
                return Promise.Delay(100.Millis())
                    .Then((res, _) => Initialized = true);
            }

            public void FailedInitialize(Exception exception = null)
            {
            }
        }

        private class InitializedComponent2 : IInitialize
        {
            [Singleton]
            public InitializedComponent2()
            {
            }

            public bool Initialized { get; set; }

            public Promise Initialize()
            {
                return Promise.Delay(200.Millis())
                    .Then((res, _) => Initialized = true);
            }

            public void FailedInitialize(Exception exception = null)
            {
            }
        }

        private class FailedInitialization : IInitialize
        {
            [Singleton]
            public FailedInitialization()
            {
            }

            public bool Initialized { get; set; }

            public Promise Initialize()
            {
                return Promise.Rejected(new InvalidOperationException(
                    "Initialization Failed"));
            }

            public void FailedInitialize(Exception exception = null)
            {
                Assert.AreEqual("Initialization Failed", exception?.Message);
            }
        }

        private class SayHello
        {
            [Handles]
            public string Hello(string name) => $"Hello {name}";
        }

        private class LifestyleMismatch
        {
            [Singleton]
            public LifestyleMismatch(Screen screen)
            {
            }
        }

        private class OverloadedConstructors
        {
            [Contextual]
            public OverloadedConstructors()
            {
            }

            [Contextual]
            public OverloadedConstructors(Foo foo)
            {
                Foo = foo;
            }

            [Contextual]
            public OverloadedConstructors(Foo foo, Bar bar)
                : this(foo)
            {
                Bar = bar;
            }

            public Foo Foo { get; }

            public Bar Bar { get; }
        }

        private class OverloadedProviders
        {
            [Provides, Contextual]
            public static OverloadedProviders Provide()
            {
                return new OverloadedProviders();
            }

            [Provides, Contextual]
            public static OverloadedProviders Provide(Foo foo)
            {
                return new OverloadedProviders { Foo = foo };
            }

            [Provides, Contextual]
            public static OverloadedProviders Provide(Foo foo, Bar bar)
            {
                return new OverloadedProviders
                {
                    Foo = foo,
                    Bar = bar
                };
            }

            public Foo Foo { get; private set; }

            public Bar Bar { get; private set; }
        }

        private class FilteredHandler : Handler, IFilter<Bar, object>
        {
            int? IOrdered.Order { get; set; }

            [Handles,
             Filter(typeof(LogFilter<,>)),
             Filter(typeof(ContravariantFilter), Required = true),
             Filter(typeof(ExceptionBehavior<,>), Required = true)]
            public void HandleBar(Bar bar)
            {
                bar.Handled++;
            }

            [Handles,
             Filter(typeof(LogFilter<,>)),
             SkipFilters]
            public void HandleBee(Bee bee)
            {
            }

            [Provides]
            public ContravariantFilter CreateFilter()
            {
                return new ContravariantFilter();
            }

            [Provides(typeof(LogFilter<,>))]
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
                Bar callback, object rawCallback,
                MemberBinding binding, IHandler composer,
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
             Filter(typeof(LogFilter<,>), Required = true),
             Filter(typeof(ContravariantFilter), Required = true),
             Filter(typeof(ExceptionBehavior<,>), Required = true)]
            public SpecialFoo HandleFoo(Foo foo)
            {
                return new SpecialFoo();
            }

            [Handles,
             Filter(typeof(LogFilter<,>), Required = true),
             Filter(typeof(ContravariantFilter), Required = true),
             Filter(typeof(ExceptionBehavior<,>), Required = true)]
            public Promise<SpecialBaz> HandleBaz(Baz baz)
            {
                return Promise.Resolved(new SpecialBaz());
            }

            [Handles,
             Filter(typeof(LogFilter<,>), Required = true),
             Filter(typeof(ContravariantFilter), Required = true),
             Filter(typeof(ExceptionBehavior<,>), Required = true)]
            public Task<SpecialBar> HandleBaz(Bar bar)
            {
                return Task.FromResult(new SpecialBar());
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

        private class ContravariantFilter : IFilter<object, object>
        {
            public int? Order { get; set; }

            public Task<object> Next(object callback,
                object rawCallback, MemberBinding member,
                IHandler composer, Next<object> next,
                IFilterProvider provider = null)
            {
                if (callback is Callback cb)
                    cb.Filters.Add(this);
                return next();
            }
        }

        private class LogFilter<Cb, Res> : IFilter<Cb, Res>
        {
            public int? Order { get; set; } = 1;

            public Task<Res> Next(Cb callback,
                object rawCallback, MemberBinding binding,
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

            public Task<Res> Next(Req request,
                object rawCallback, MemberBinding binding,
                IHandler composer, Next<Res> next,
                IFilterProvider provider)
            {
                var cb = ExtractCallback(request);
                cb?.Filters.Add(this);
                var result = next();
                if (request is Boo)
                    return Promise<Res>.Rejected(
                        new InvalidOperationException("System shutdown"));
                return result;
            }
        }

        private class ReplaceComposerFilter<Cb, Res> : IFilter<Cb, Res>
        {
            public int? Order { get; set; } = Stage.Filter;

            public Task<Res> Next(Cb callback,
                object rawCallback, MemberBinding binding,
                IHandler composer, Next<Res> next,
                IFilterProvider provider)
            {
                return next(composer.Provide(new Bar()));
            }
        }
    }
}
