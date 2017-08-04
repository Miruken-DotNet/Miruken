namespace Miruken.Tests.Context
{
    using System;
    using System.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Context;
    using static Protocol;

    /// <summary>
    /// Summary description for ContextTests
    /// </summary>
    [TestClass]
    public class ContextTests
    {
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
            var child1     = context.CreateChild();
            var grandChild = child1.CreateChild();
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
            Assert.IsNull(child2.SelfDescendant().Resolve<DataTable>());
            Assert.AreSame(data, grandChild.SelfDescendant().Resolve<DataTable>());
            Assert.AreSame(data, child3.SelfDescendant().Resolve<DataTable>());
            Assert.AreSame(data, root.SelfDescendant().Resolve<DataTable>());
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
            Assert.IsNull(child2.SelfDescendant().Resolve<DataTable>());
            Assert.AreSame(data, root.SelfDescendant().Resolve<DataTable>());
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

            Func<IHandler, IHandler> Foo =
                h => h.Aspect((_, __, ___) => ++count);

            Assert.IsNull(Foo(child2.SelfDescendant()).Resolve<DataTable>());
            Assert.AreSame(data, Foo(grandChild.SelfDescendant()).Resolve<DataTable>());
            Assert.AreSame(data, Foo(child3.SelfDescendant()).Resolve<DataTable>());
            Assert.AreSame(data, Foo(root.SelfDescendant()).Resolve<DataTable>());
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
            root.AddHandlers(new Observer(count));
            child1.AddHandlers(new Observer(count), new Observer(count));
            child2.AddHandlers(new Observer(count));
            child3.AddHandlers(new Observer(count), new Observer(count));
            Proxy<IObserving>(root.Publish()).Observe();
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
            root.AddHandlers(new Observer(count));
            child1.AddHandlers(new Observer(count), new Observer(count));
            child2.AddHandlers(new Observer(count));
            child3.AddHandlers(new Observer(count), new Observer(count));
            Proxy<IObserving>(root.Publish()).Observe();
            Assert.AreEqual(6, count.Count);
        }

        [TestMethod]
        public void Should_Publish_Reverse_Descendants_Using_Protocol_BestEffort()
        {
            var root = new Context();
            var child1 = root.CreateChild();
            var child2 = root.CreateChild();
            var child3 = root.CreateChild();
            var grandChild = child3.CreateChild();
            Proxy<IObserving>(root.SelfOrDescendantReverse().BestEffort()).Observe();
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
            void Observe();
        }

        private class Observer : Handler, IObserving
        {
            private readonly Counter _counter;

            public Observer(Counter counter)
            {
                _counter = counter;
            }

            public void Observe()
            {
                _counter.Increment();
            }
        }
    }
}
