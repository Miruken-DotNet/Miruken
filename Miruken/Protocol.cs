using System.Runtime.Remoting.Messaging;
using Miruken.Callback;

namespace Miruken
{
    public interface IStrict { }

    public interface IProtocolAdapter
    {
        object Dispatch(IMethodCallMessage message);
    }

    #region NullProtocolAdapter

    public class NullProtocolAdapter : IProtocolAdapter
    {
        public static readonly NullProtocolAdapter
            Instance = new NullProtocolAdapter();

        private NullProtocolAdapter()
        {
        }

        public object Dispatch(IMethodCallMessage message)
        {
            return null;
        }
    }

    #endregion

    public static class Protocol
    {
        public static object P(IProtocolAdapter adapter)
        {
            return new Interceptor(adapter).GetTransparentProxy();
        }

        public static TProtocol P<TProtocol>(IProtocolAdapter adapter)
        {
            return (TProtocol)new Interceptor(adapter).GetTransparentProxy();
        }

        public static object As(this IProtocolAdapter adapter)
        {
            return P(adapter);
        }

        public static TProtocol As<TProtocol>(this IProtocolAdapter adapter)
        {
            return P<TProtocol>(adapter);
        }

        public static IHandler Composer => HandleMethod.Composer;
        public static bool Unhandled => HandleMethod.Unhandled;
    }
}
