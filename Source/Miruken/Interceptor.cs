using System.Collections.Concurrent;
using Miruken.Callback;
using Miruken.Infrastructure;

namespace Miruken
{
    using System;
    using System.Reflection;

    public class Interceptor : DispatchProxy
    {
        private IProtocolAdapter _adapter;
        private Type _protocol;

        private static readonly MethodInfo CreateProxy =
            typeof(DispatchProxy).GetMethod("Create",
                BindingFlags.Public | BindingFlags.Static |
                BindingFlags.DeclaredOnly);

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
                (Func<object>)RuntimeHelper.CompileMethod(
                    CreateProxy.MakeGenericMethod(protocol, typeof(Interceptor)),
                    typeof(Func<object>))
            );
            var interceptor = (Interceptor)factory();
            interceptor._protocol = protocol;
            interceptor._adapter = adapter;
            return interceptor;
        }

        public static T Create<T>(IProtocolAdapter adapter)
        {
            var proxy       = Create<T, Interceptor>();
            var interceptor = (Interceptor)(object)proxy;
            interceptor._protocol = typeof(T);
            interceptor._adapter = adapter;
            return proxy;
        }

        private static readonly ConcurrentDictionary<Type, Func<object>> 
            Factories = new ConcurrentDictionary<Type, Func<object>>();
    }
}
