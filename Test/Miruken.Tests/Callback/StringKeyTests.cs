namespace Miruken.Tests.Callback
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Callback;

    [TestClass]
    public class StringKeyTests
    {
        [TestMethod]
        public void Should_Match_Null()
        {
            var key = new StringKey(null);
            Assert.IsTrue(key.Equals(null));
        }

        [TestMethod]
        public void Should_Match_Key_With_Null()
        {
            var key = new StringKey(null);
            Assert.IsTrue(key.Equals(new StringKey(null)));
        }

        [TestMethod]
        public void Should_Match_Raw_String()
        {
            var key = new StringKey("abc");
            Assert.IsTrue(Equals(key, "abc"));
        }

        [TestMethod]
        public void Should_Match_Other_Key()
        {
            var key = new StringKey("abc");
            Assert.IsTrue(Equals(key, new StringKey("abc")));
        }

        [TestMethod]
        public void Should_Match_Raw_String_With_Comparison()
        {
            var key = new StringKey("abc", StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(Equals(key, "ABC"));
        }

        [TestMethod]
        public void Should_Delegate_HashCode()
        {
            Assert.AreEqual(0, new StringKey(null).GetHashCode());
            Assert.AreEqual("abc".GetHashCode(), 
                new StringKey("abc").GetHashCode());
        }
    }
}
