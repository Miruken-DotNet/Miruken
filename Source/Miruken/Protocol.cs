using Miruken.Callback;

namespace Miruken
{
    using System;

    public interface IProtocol {}

    public interface IDuck : IProtocol {}

    public interface IStrict : IProtocol {}

    public interface IProtocolAdapter
    {
        object Dispatch(HandleMethod method);
    }

    public static class Protocol
    {
        public static object Proxy(IProtocolAdapter adapter, Type protocol)
        {
            if (!protocol.IsInterface)
                throw new NotSupportedException("Only protocol interfaces are supported");
            return Interceptor.Create(adapter, protocol);
        }

        public static TProto Proxy<TProto>(IProtocolAdapter adapter)
        {
            if (!typeof(TProto).IsInterface)
                throw new NotSupportedException("Only protocol interfaces are supported");
            return Interceptor.Create<TProto>(adapter);
        }
    }

    public static class ProtocolExtensions
    {
        public static object Proxy(this IProtocolAdapter adapter, Type protocolType)
        {
            return Protocol.Proxy(adapter, protocolType);
        }

        public static TProto Proxy<TProto>(this IProtocolAdapter adapter)
            where TProto : class
        {
            return Protocol.Proxy<TProto>(adapter);
        }
    }
}
