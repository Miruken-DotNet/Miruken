namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using Bindings;
    using Infrastructure;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class HandlerDescriptor : FilteredObject
    {
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
        }

        public Type HandlerType { get; }

        public Attribute[] Attributes { get; }

        public int? Priority { get; set; }

        public IDictionary<CallbackPolicy, CallbackPolicyDescriptor> Policies       { get; }
        public IDictionary<CallbackPolicy, CallbackPolicyDescriptor> StaticPolicies { get; }

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

            var dispatched     = false;
            var hasConstructor = false;
            var key            = policy.GetKey(callback);

            foreach (var member in descriptor.GetInvariantMembers(key))
            {
                if (!member.Approves(callback)) continue;
                var isConstructor = member.Dispatcher.IsConstructor;
                if (isConstructor && hasConstructor) continue;
                dispatched = member.Dispatch(target, callback, composer, Priority, results)
                          || dispatched;
                if (dispatched)
                {
                    if (!greedy) return true;
                    hasConstructor |= isConstructor;
                }
            }

            foreach (var member in descriptor.GetCompatibleMembers(key))
            {
                if (!member.Approves(callback)) continue;
                var isConstructor = member.Dispatcher.IsConstructor;
                if (isConstructor && hasConstructor) continue;
                dispatched = member.Dispatch( target, callback, composer, Priority, results)
                          || dispatched;
                if (dispatched)
                {
                    if (!greedy) return true;
                    hasConstructor |= isConstructor;
                }
            }

            return dispatched;
        }

        private string DebuggerDisplay => $"{HandlerType.FullName}";
    }
}
