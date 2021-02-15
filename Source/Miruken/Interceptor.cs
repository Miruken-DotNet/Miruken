namespace Miruken
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using Callback;
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
            Factories = new();
    }
}
