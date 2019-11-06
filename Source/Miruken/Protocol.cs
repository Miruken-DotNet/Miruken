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

#if NETSTANDARD
            return Interceptor.Create(adapter, protocol);
#else
            return new Interceptor(adapter, protocol).GetTransparentProxy();
#endif
        }

        public static TProto Proxy<TProto>(IProtocolAdapter adapter)
        {
#if NETSTANDARD
            if (!typeof(TProto).IsInterface)
                throw new NotSupportedException("Only protocol interfaces are supported");
            return Interceptor.Create<TProto>(adapter);
#else
            return (TProto)Proxy(adapter, typeof(TProto));
#endif
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
