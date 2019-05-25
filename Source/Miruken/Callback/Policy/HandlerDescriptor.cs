namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Bindings;
    using Infrastructure;
    
    public class HandlerDescriptor : FilteredObject
    {
        private readonly ConcurrentDictionary<object, Type> _closed;

        private static readonly IDictionary<CallbackPolicy, CallbackPolicyDescriptor> Empty =
            new ReadOnlyDictionary<CallbackPolicy, CallbackPolicyDescriptor>(
                new Dictionary<CallbackPolicy, CallbackPolicyDescriptor>());

        public HandlerDescriptor(Type handlerType,
            IDictionary<CallbackPolicy, CallbackPolicyDescriptor> policies,
            IDictionary<CallbackPolicy, CallbackPolicyDescriptor> staticPolicies)
        {
            if (handlerType == null)
                throw new ArgumentNullException(nameof(handlerType));

            if (!handlerType.IsClass)
                throw new ArgumentException("Only classes can be handlers");

            HandlerType    = handlerType;
            Policies       = policies ?? Empty;
            StaticPolicies = staticPolicies ?? Empty;
            Attributes     = Attribute.GetCustomAttributes(handlerType, true).Normalize();

            AddFilters(Attributes.OfType<IFilterProvider>().ToArray());

            if (handlerType.IsGenericTypeDefinition)
                _closed = new ConcurrentDictionary<object, Type>();
        }

        public Type HandlerType { get; }

        public Attribute[] Attributes { get; }

        public bool IsOpenGeneric => HandlerType.IsGenericTypeDefinition;

       public IDictionary<CallbackPolicy, CallbackPolicyDescriptor> Policies { get; private set; }
       public IDictionary<CallbackPolicy, CallbackPolicyDescriptor> StaticPolicies { get; private set; }

        public Type CloseType(object key, PolicyMemberBinding binding)
        {
            return _closed.GetOrAdd(key, k => binding.CloseHandlerType(HandlerType, k));
        }

        internal bool Dispatch(
            CallbackPolicy policy, object target, 
            object callback, bool greedy, IHandler composer,
            ResultsDelegate results = null)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            CallbackPolicyDescriptor descriptor = null;
            var policies = target == null || target is Type ? StaticPolicies : Policies;
            if (policies?.TryGetValue(policy, out descriptor) != true)
                return false;

            var dispatched = false;
            foreach (var member in descriptor.GetInvariantMembers(callback))
            {
                dispatched = member.Dispatch(
                    target, callback, composer, results) || dispatched;
                if (dispatched && !greedy) return true;
            }

            foreach (var member in descriptor.GetCompatibleMembers(callback))
            {
                dispatched = member.Dispatch(
                    target, callback, composer, results) || dispatched;
                if (dispatched && !greedy) return true;
            }

            return dispatched;
        }
    }
}
