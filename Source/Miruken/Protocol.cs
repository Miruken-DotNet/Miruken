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

    public class Protocol
    {
        public static object Cast(IProtocolAdapter adapter)
        {
            return new Interceptor(adapter).GetTransparentProxy();
        }
    }

    public class Protocol<TProto> where TProto : class
    {
        public static TProto Cast(IProtocolAdapter adapter)
        {
            if (!typeof(TProto).IsInterface)
                throw new NotSupportedException("Only protocol interfaces are supported");
            return (TProto)new Interceptor(adapter, typeof(TProto))
                .GetTransparentProxy();
        }
    }

    public static class ProtocolExtensions
    {
        public static object Cast(this IProtocolAdapter adapter)
        {
            return Protocol.Cast(adapter);
        }

        public static TProto Cast<TProto>(this IProtocolAdapter adapter)
            where TProto : class
        {
            return Protocol<TProto>.Cast(adapter);
        }
    }
}
