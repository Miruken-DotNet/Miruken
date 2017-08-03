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
        public static object id(IProtocolAdapter adapter)
        {
            return new Interceptor(adapter).GetTransparentProxy();
        }

        public static TProto id<TProto>(IProtocolAdapter adapter)
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
        public static object id(this IProtocolAdapter adapter)
        {
            return Protocol.id(adapter);
        }

        public static TProto id<TProto>(this IProtocolAdapter adapter)
            where TProto : class
        {
            return Protocol.id<TProto>(adapter);
        }
    }
}
