namespace Miruken
{
    using System;
    using System.Reflection;
    using Callback;

#if NETSTANDARD
    using System.Collections.Concurrent;
    using Infrastructure;

    public class Interceptor : DispatchProxy
    {
        private IProtocolAdapter _adapter;
        private Type _protocol;

        private static readonly MethodInfo CreateProxy =
            typeof(Interceptor).GetMethod("Create",
                new [] {typeof(IProtocolAdapter) });

        protected override object Invoke(MethodInfo method, object[] args)
        {
            try
            {
                var handleMethod = new HandleMethod(_protocol, method, args);
                return _adapter.Dispatch(handleMethod);
            }
            catch (TargetInvocationException tex)
            {
                throw tex.InnerException ?? tex;
            }
        }

        public static object Create(IProtocolAdapter adapter, Type protocol)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));
            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));
            var factory = Factories.GetOrAdd(protocol, p =>
                (Func<object, object>)RuntimeHelper.CompileMethod(
                    CreateProxy.MakeGenericMethod(p),
                    typeof(Func<object, object>))
            );
            return (Interceptor)factory(adapter);
        }

        public static T Create<T>(IProtocolAdapter adapter)
        {
            var proxy       = Create<T, Interceptor>();
            var interceptor = (Interceptor)(object)proxy;
            interceptor._protocol = typeof(T);
            interceptor._adapter = adapter;
            return proxy;
        }

        private static readonly ConcurrentDictionary<Type, Func<object, object>> 
            Factories = new ConcurrentDictionary<Type, Func<object, object>>();
    }
#elif NETFULL
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Messaging;
    using System.Runtime.Remoting.Proxies;

    public class Interceptor : RealProxy, IRemotingTypeInfo
    {
        private readonly IProtocolAdapter _adapter;
        private readonly Type _protocol;

        public Interceptor(IProtocolAdapter adapter, Type protocol = null)
            : base(typeof(MarshalByRefObject))
        {
            _adapter = adapter 
                ?? throw new ArgumentNullException(nameof(adapter));
            _protocol = protocol;
        }

        public string TypeName { get; set; }

        public bool CanCastTo(Type fromType, object o)
        {
            return _protocol == null || fromType.IsAssignableFrom(_protocol);
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = (IMethodCallMessage) msg;
            try
            {
                var method       = (MethodInfo)methodCall.MethodBase;
                var handleMethod = new HandleMethod(_protocol, method, methodCall.Args);
                return new ReturnMessage(
                    _adapter.Dispatch(handleMethod),
                    methodCall.Args, methodCall.ArgCount,
                    methodCall.LogicalCallContext, methodCall);
            }
            catch (TargetInvocationException tex)
            {
                return new ReturnMessage(tex.InnerException, methodCall);
            }
        }

        public override ObjRef CreateObjRef(Type type)
        {
            throw new NotSupportedException();
        }
    }
#endif
}
