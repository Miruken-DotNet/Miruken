using System.Runtime.Remoting.Messaging;
using Miruken.Callback;

namespace Miruken
{
    using System;

    public interface IDuck
    {
    }

    public interface IProtocolAdapter
    {
        object Dispatch(Type protocol, IMethodCallMessage message);
    }

    public static class Protocol
    {
        public static object P(IProtocolAdapter adapter)
        {
            return new Interceptor(adapter).GetTransparentProxy();
        }

        public static TProtocol P<TProtocol>(IProtocolAdapter adapter)
        {
            return (TProtocol) P(adapter);
        }

        public static IHandler Composer => HandleMethod.Composer;
        public static bool Unhandled => HandleMethod.Unhandled;
    }

    public static class ProtocolExtensions
    {
        public static object P(this IProtocolAdapter adapter)
        {
            return Protocol.P(adapter);
        }

        public static TProtocol P<TProtocol>(this IProtocolAdapter adapter)
        {
            return Protocol.P<TProtocol>(adapter);
        }
    }
}
