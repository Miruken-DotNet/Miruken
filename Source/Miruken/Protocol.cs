namespace Miruken
{
    using System;
    using System.Runtime.Remoting.Messaging;

    public interface IProtocol {}

    public interface IDuck : IProtocol {}

    public interface IStrict : IProtocol {}

    public interface IProtocolAdapter
    {
        object Dispatch(Type protocol, IMethodCallMessage message);
    }

    public static class Protocol
    {
        public static object Proxy(IProtocolAdapter adapter)
        {
            return new Interceptor(adapter).GetTransparentProxy();
        }

        public static object Proxy(IProtocolAdapter adapter, Type protocol)
        {
            if (!protocol.IsInterface)
                throw new NotSupportedException("Only protocol interfaces are supported");
            return new Interceptor(adapter, protocol).GetTransparentProxy();
        }

        public static TProto Proxy<TProto>(IProtocolAdapter adapter)
        {
            return (TProto)Proxy(adapter, typeof(TProto));
        }
    }

    public static class ProtocolExtensions
    {
        public static object Proxy(this IProtocolAdapter adapter)
        {
            return Protocol.Proxy(adapter);
        }

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
