namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Reflection;
    using Concurrency;
    using Infrastructure;

    public class MethodDispatch : MemberDispatch
    {
        public MethodDispatch(
            MethodInfo method, Attribute[] attributes = null)
            : base(method, method.ReturnType, attributes)
        {
        }

        public MethodInfo Method => (MethodInfo)Member;

        public override object Invoke(object target, object[] args, Type returnType = null)
        {
            if (!IsPromise)
                return Dispatch(target, args, returnType);

            try
            {
                return Dispatch(target, args, returnType);
            }
            catch (Exception exception)
            {
                if (exception is TargetException tie)
                    exception = tie.InnerException;
                return Promise.Rejected(exception).Coerce(ReturnType);
            }
        }

        private object Dispatch(object target, object[] args, Type type)
        {
            if (IsLateBound)
                return Method.Invoke(target, Binding, null, args, CultureInfo.InvariantCulture);

            var delegateFlags = DispatchType & DispatchTypeEnum.DelegateMask;
            
            var @delegate = Delegates.GetOrAdd(Method, m => 
                MemberDelegates.TryGetValue(delegateFlags, out var invoker)
                    ? RuntimeHelper.CompileMethod(m, invoker.Item1)
                    : null);
            
            if (@delegate == null)
                throw new InvalidOperationException($"Unable to create delegate for method {Method}.");
        
            return MemberDelegates[delegateFlags].Item2(@delegate, target, args);
        }
        
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> Delegates = new ();
    }
}
