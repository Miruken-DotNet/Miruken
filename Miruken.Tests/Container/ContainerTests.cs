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
                return null;
            }

            T[] IContainer.ResolveAll<T>()
            {
                return new T[0];
            }

            object[] IContainer.ResolveAll(object key)
            {
                return new object[0];
            }

            void IContainer.Release(object component)
            {
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            var handler = new TestContainer();
            var car = P<IContainer>(handler).ResolveAll<Car>();
            car = handler.As<IContainer>().ResolveAll<Car>();
        }

        public interface Car
        {
            string Make { get; set; }

            string Model { get; set; }
        }
    }
}
