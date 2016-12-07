using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Callback;
using Miruken.Container;
using static Miruken.Protocol;

namespace Miruken.Tests.Container
{
    [TestClass]
    public class ContainerTests
    {
        public class TestContainer : Handler, IContainer
        {
            T IContainer.Resolve<T>()
            {
                return default(T);
            }

            object IContainer.Resolve(object key)
            {
                throw new System.NotImplementedException();
            }

            T[] IContainer.ResolveAll<T>()
            {
                throw new System.NotImplementedException();
            }

            object[] IContainer.ResolveAll(object key)
            {
                throw new System.NotImplementedException();
            }

            void IContainer.Release(object component)
            {
                throw new System.NotImplementedException();
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            var handler = new TestContainer();
            P<IContainer>(handler).Resolve<Car>();
        }

        public interface Car
        {
            string Make { get; set; }

            string Model { get; set; }
        }
    }
}
