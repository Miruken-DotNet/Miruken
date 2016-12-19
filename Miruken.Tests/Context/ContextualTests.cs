namespace Miruken.Tests.Context
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;
    using Miruken.Context;

    [TestClass]
    public class ContextualTests
    {
        private Context _rootContext;

        public class MyService : Contextual
        {
            
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _rootContext = new Context();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _rootContext.End();
        }

        [TestMethod]
        public void Should_Add_Contextual_To_Context_When_Assigned()
        {
            var service  = new MyService { Context = _rootContext };
            var services = _rootContext.ResolveAll<MyService>();
            Assert.AreEqual(1, services.Length);
        }
    }
}
