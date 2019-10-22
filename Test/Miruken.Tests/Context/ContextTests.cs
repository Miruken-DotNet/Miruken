namespace Miruken.Tests.Context
{
    using System;
    using System.Data;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Callback.Policy;
    using Miruken.Context;
    using static Protocol;

    /// <summary>
    /// Summary description for ContextTests
    /// </summary>
    [TestClass]
    public class ContextTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            var factory = new MutableHandlerDescriptorFactory();
            factory.RegisterDescriptor<Counter>();
            factory.RegisterDescriptor<Observer>();
            factory.RegisterDescriptor<ServiceProvider>();
            HandlerDescriptorFactory.UseFactory(factory);
        }

        [TestMethod]
        public void Should_Start_In_Active_State()
        {
            var context = new Context();
            Assert.AreEqual(ContextState.Active, context.State);
        }

        [TestMethod]
        public void Should_Get_Self()
        {
            var context = new Context();
            Assert.AreSame(context, context.Resolve<Context>());
        }

        [TestMethod]
        public void Should_Get_Self_For_ServiceProvider()
        {
            var context = new Context();
            Assert.AreSame(context, context.Resolve<IServiceProvider>());
        }

#if NETSTANDARD
        [TestMethod]
        public void Should_Get_Self_For_ServiceScopeFactory()
        {
            var context = new Context();
            Assert.AreSame(context, context.Resolve<IServiceScopeFactory>());
        }
#endif

#if NETSTANDARD
        [TestMethod]
        public void Should_Create_Service_Scope()
        {
            var context = new Context();
            var scope   = context.CreateScope();
            Assert.IsNotNull(scope);
        }
#endif

        [TestMethod]
        public void Should_Not_Have_Parent_If_Root()
        {
            var context = new Context();
            Assert.IsNull(context.Parent);
        }

        [TestMethod]
        public void Should_Get_Root_Context()
        {
            var context = new Context();
            var child   = context.CreateChild();
            Assert.AreSame(context, context.Root);
            Assert.AreSame(context, child.Root);
        }

        [TestMethod]
        public void Should_Have_Parent_If_Child()
        {
            var context = new Context();
            var child   = context.CreateChild();
            Assert.AreSame(context, child.Parent);
        }

        [TestMethod]
        public void Should_Not_Have_Children_By_Default()
        {
            var context = new Context();
            Assert.IsFalse(context.HasChildren);
        }

        [TestMethod]
        public void Should_Have_Children_If_Created()
        {
            var context = new Context();
            var child1  = context.CreateChild();
            var child2  = context.CreateChild();
            Assert.IsTrue(context.HasChildren);
            CollectionAssert.AreEqual(context.Children, new [] { child1, child2 });
        }

        [TestMethod]
        public void Should_End_Context()
        {
            var context = new Context();
            context.End();
            Assert.AreEqual(ContextState.Ended, context.State);
        }

        [TestMethod]
        public void Should_End_Child_Context()
        {
            var context = new Context();
            var child   = context.CreateChild();
            context.End();
            Assert.AreEqual(ContextState.Ended, child.State);
        }

        [TestMethod]
        public void Should_End_Context_If_Disposed()
        {
            var context = new Context();
            ((IDisposable)context).Dispose();
            Assert.AreEqual(ContextState.Ended, context.State);
        }

        [TestMethod]
        public void Should_Unwind_Children()
        {
            var context = new Context();
            var child1  = context.CreateChild();
            var child2  = context.CreateChild();
            context.Unwind();
            Assert.AreEqual(ContextState.Active, context.State);
            Assert.AreEqual(ContextState.Ended, child1.State);
            Assert.AreEqual(ContextState.Ended, child2.State);
        }

        [TestMethod]
        public void Should_Unwind_To_Root()
        {
            var context    = new Context();
            var child1     = context.CreateChild();
            var child2     = context.CreateChild();
            var grandChild = child1.CreateChild();
            var root = child2.UnwindToRoot();
            Assert.AreSame(context, root);
            Assert.AreEqual(ContextState.Active, context.State);
            Assert.AreEqual(ContextState.Ended, child1.State);
            Assert.AreEqual(ContextState.Ended, child2.State);
            Assert.AreEqual(ContextState.Ended, grandChild.State);
        }

        [TestMethod]
        public void Should_Store_Object()
        {
            var data    = new DataTable();
            var context = new Context();
            context.Store(data);
            Assert.AreSame(data, context.Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Ancestors_By_Default()
        {
            var data       = new DataTable();
            var context    = new Context();
            var child      = context.CreateChild();
            var grandChild = child.CreateChild();
            context.Store(data);
            Assert.AreEqual(data, grandChild.Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Self()
        {
            var data  = new DataTable();
            var root  = new Context();
            var child = root.CreateChild();
            root.Store(data);
            Assert.IsNull(child.Self().Resolve<DataTable>());
            Assert.AreSame(data, root.Self().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Root()
        {
            var data  = new DataTable();
            var root  = new Context();
            var child = root.CreateChild();
            child.Store(data);
            Assert.IsNull(child.Root().Resolve<DataTable>());
            root.Store(data);
            Assert.AreSame(data, root.Root().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Children()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            child2.Store(data);
            Assert.IsNull(child2.Child().Resolve<DataTable>());
            Assert.IsNull(grandChild.Child().Resolve<DataTable>());
            Assert.AreSame(data, root.Child().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Siblings()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            child3.Store(data);
            Assert.IsNull(root.Sibling().Resolve<DataTable>());
            Assert.IsNull(child3.Sibling().Resolve<DataTable>());
            Assert.IsNull(grandChild.Sibling().Resolve<DataTable>());
            Assert.AreSame(data, child2.Sibling().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Children_Or_Self()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            child3.Store(data);
            Assert.IsNull(child1.SelfOrChild().Resolve<DataTable>());
            Assert.IsNull(grandChild.SelfOrChild().Resolve<DataTable>());
            Assert.AreSame(data, child3.SelfOrChild().Resolve<DataTable>());
            Assert.AreSame(data, root.SelfOrChild().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Sibling_Or_Self()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            child3.Store(data);
            Assert.IsNull(root.SelfOrSibling().Resolve<DataTable>());
            Assert.IsNull(grandChild.SelfOrSibling().Resolve<DataTable>());
            Assert.AreSame(data, child3.SelfOrSibling().Resolve<DataTable>());
            Assert.AreSame(data, child2.SelfOrSibling().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Ancestors()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child      = root.CreateChild();
            var grandChild = child.CreateChild();
            root.Store(data);
            Assert.IsNull(root.Ancestor().Resolve<DataTable>());
            Assert.AreSame(data, grandChild.Ancestor().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Ancestors_Or_Self()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child      = root.CreateChild();
            var grandChild = child.CreateChild();
            root.Store(data);
            Assert.AreSame(data, root.SelfOrAncestor().Resolve<DataTable>());
            Assert.AreSame(data, grandChild.SelfOrAncestor().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Descendants()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            grandChild.Store(data);
            Assert.IsNull(grandChild.Descendant().Resolve<DataTable>());
            Assert.IsNull(child2.Descendant().Resolve<DataTable>());
            Assert.AreSame(data, child3.Descendant().Resolve<DataTable>());
            Assert.AreSame(data, root.Descendant().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Descendants_Or_Self()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            grandChild.Store(data);
            Assert.IsNull(child2.SelfOrDescendant().Resolve<DataTable>());
            Assert.AreSame(data, grandChild.SelfOrDescendant().Resolve<DataTable>());
            Assert.AreSame(data, child3.SelfOrDescendant().Resolve<DataTable>());
            Assert.AreSame(data, root.SelfOrDescendant().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Descendants_Or_SELF()
        {
            var data = new DataTable();
            var root = new Context();
            var child1 = root.CreateChild();
            var child2 = root.CreateChild();
            var child3 = root.CreateChild();
            var grandChild = child3.CreateChild();
            root.Store(data);
            Assert.IsNull(child2.SelfOrDescendant().Resolve<DataTable>());
            Assert.AreSame(data, root.SelfOrDescendant().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_Ancestors_SIBLINGS_Or_Self()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            child2.Store(data);
            Assert.IsNull(grandChild.SelfSiblingOrAncestor().Resolve<DataTable>());
            Assert.AreSame(data, child3.SelfSiblingOrAncestor().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Traverse_ANCESTORS_Siblings_Or_Self()
        {
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            child3.Store(data);
            Assert.AreSame(data, grandChild.SelfSiblingOrAncestor().Resolve<DataTable>());
        }

        [TestMethod]
        public void Should_Combine_Aspects_With_Traversal()
        {
            var count      = 0;
            var data       = new DataTable();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            grandChild.Store(data);

            IHandler Foo(IHandler h) => h.Aspect((_, __, ___) => ++count);

            Assert.IsNull(Foo(child2.SelfOrDescendant()).Resolve<DataTable>());
            Assert.AreSame(data, Foo(grandChild.SelfOrDescendant()).Resolve<DataTable>());
            Assert.AreSame(data, Foo(child3.SelfOrDescendant()).Resolve<DataTable>());
            Assert.AreSame(data, Foo(root.SelfOrDescendant()).Resolve<DataTable>());
            Assert.AreEqual(4, count);
        }

        [TestMethod]
        public void Should_Publish_To_All_Descendants()
        {
            var count      = new Counter();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            root.AddHandlers(new Observer());
            child1.AddHandlers(new Observer(), new Observer());
            child2.AddHandlers(new Observer());
            child3.AddHandlers(new Observer(), new Observer());
            Proxy<IObserving>(root.Publish()).Observe(count);
            Assert.AreEqual(6, count.Count);
        }

        [TestMethod]
        public void Should_Publish_To_All_Descendants_Using_Protocol()
        {
            var count      = new Counter();
            var root       = new Context();
            var child1     = root.CreateChild();
            var child2     = root.CreateChild();
            var child3     = root.CreateChild();
            var grandChild = child3.CreateChild();
            root.AddHandlers(new Observer());
            child1.AddHandlers(new Observer(), new Observer());
            child2.AddHandlers(new Observer());
            child3.AddHandlers(new Observer(), new Observer());
            Proxy<IObserving>(root.Publish()).Observe(count);
            Assert.AreEqual(6, count.Count);
        }

        [TestMethod]
        public void Should_Publish_Reverse_Descendants_Using_Protocol_BestEffort()
        {
            var root   = new Context();
            var child1 = root.CreateChild();
            var child2 = root.CreateChild();
            var child3 = root.CreateChild();
            var grandChild = child3.CreateChild();
            Proxy<IObserving>(root.SelfOrDescendantReverse()
                .BestEffort()).Observe(new Counter());
        }

        [TestMethod]
        public void Should_Publish_From_Root_Context()
        {
            var count  = new Counter();
            var root   = new Context();
            new Observer().Context = root;
            var child1 = root.CreateChild();
            new Observer().Context = child1;
            var child2 = root.CreateChild();
            new Observer().Context = child2;
            var child3 = root.CreateChild();
            new Observer().Context = child3;
            var grandChild = child3.CreateChild();
            new Observer().Context = grandChild;
            child2.PublishFromRoot().Handle(count);
            Assert.AreEqual(5, count.Count);
        }

        private class Counter
        {
            public int Count { get; private set; }
            public int Increment()
            {
                return ++Count;
            }
        }

        private interface IObserving
        {
            void Observe(Counter counter);
        }

        private class Observer : ContextualHandler, IObserving
        {
            public void Observe(Counter counter)
            {
                counter.Increment();
            }

            [Handles]
            private void Observe(Counter counter, IHandler composer)
            {
                var context = composer.Resolve<Context>();
                Assert.AreSame(Context.Root, context);
                counter.Increment();
            }
        }
    }
}
