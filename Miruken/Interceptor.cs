using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace Miruken
{
    public interface IStrict {}

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

    public class Interceptor : RealProxy, IRemotingTypeInfo
    {
        private readonly IProtocolAdapter _adapter;

        public Interceptor(IProtocolAdapter adapter)
            : base(typeof(MarshalByRefObject))
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));
            _adapter = adapter;
        }

        public string TypeName { get; set; }

        public bool CanCastTo(Type fromType, object o)
        {
            return true;
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = (IMethodCallMessage) msg;
            try
            {
                return new ReturnMessage(
                    _adapter.Dispatch(methodCall),
                    methodCall.Args, methodCall.ArgCount,
                    methodCall.LogicalCallContext, methodCall);
            }
            catch (TargetInvocationException tex)
            {
                return new ReturnMessage(tex.InnerException, methodCall);
            }
        }
    }
}
