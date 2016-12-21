using System.Runtime.Remoting.Messaging;
using Miruken.Callback;

namespace Miruken
{
    using System;

    public interface IDuck
    {
    }

    public interface IStrict
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

        public static TProto P<TProto>(IProtocolAdapter adapter)
            where TProto : class
        {
            if (!typeof(TProto).IsInterface)
                throw new NotSupportedException("Only protocol interfaces are supported");
            return (TProto)new Interceptor(adapter, typeof(TProto))
                .GetTransparentProxy();
        }

        public static IHandler Composer  => HandleMethod.Composer;
        public static bool     Unhandled => HandleMethod.Unhandled;
    }

    public static class ProtocolExtensions
    {
        public static object P(this IProtocolAdapter adapter)
        {
            return Protocol.P(adapter);
        }

        public static TProto P<TProto>(this IProtocolAdapter adapter)
            where TProto : class
        {
            return Protocol.P<TProto>(adapter);
        }
    }
}
