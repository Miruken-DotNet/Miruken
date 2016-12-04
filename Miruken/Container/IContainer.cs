using System.Runtime.InteropServices;
using SixFlags.CF.Miruken.Callback;

namespace SixFlags.CF.Miruken.Container
{
    #region Protocol
    [ComImport,
     Guid(Protocol.Guid),
     CoClass(typeof(ContainerProtocol))]
    #endregion
    public interface IContainer
    {
        #region Resolve

        T        Resolve<T>();

        object   Resolve(object key);

        T[]      ResolveAll<T>();

        object[] ResolveAll(object key);

        void     Release(object component);

        #endregion

    }

    #region ContainerProtocol

    public class ContainerProtocol : Protocol, IContainer
    {
        public ContainerProtocol(IProtocolAdapter adapter)
            : base(adapter)
        {       
        }

        T IContainer.Resolve<T>()
        {
            return Do((IContainer p) => p.Resolve<T>());
        }

        object IContainer.Resolve(object key)
        {
            return Do((IContainer p) => p.Resolve(key));
        }

        T[] IContainer.ResolveAll<T>()
        {
            return Do((IContainer p) => p.ResolveAll<T>());
        }

        object[] IContainer.ResolveAll(object key)
        {
            return Do((IContainer p) => p.ResolveAll(key));
        }

        void IContainer.Release(object component)
        {
            Do((IContainer p) => p.Release(component));
        }
    }

    #endregion
}
