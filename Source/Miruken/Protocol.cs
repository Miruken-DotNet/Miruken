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
        public static object protocol(IProtocolAdapter adapter)
        {
            return new Interceptor(adapter).GetTransparentProxy();
        }

        public static TProto protocol<TProto>(IProtocolAdapter adapter)
            where TProto : class
        {
            if (!typeof(TProto).IsInterface)
                throw new NotSupportedException("Only protocol interfaces are supported");
            return (TProto)new Interceptor(adapter, typeof(TProto))
                .GetTransparentProxy();
        }
    }

    public static class ProtocolExtensions
    {
        public static object protocol(this IProtocolAdapter adapter)
        {
            return Protocol.protocol(adapter);
        }

        public static TProto protocol<TProto>(this IProtocolAdapter adapter)
            where TProto : class
        {
            return Protocol.protocol<TProto>(adapter);
        }
    }
}
