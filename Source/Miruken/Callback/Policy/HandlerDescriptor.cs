namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using Bindings;
    using Infrastructure;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class HandlerDescriptor : FilteredObject
    {
        private readonly HandlerDescriptorVisitor _visitor;
        private readonly ConcurrentDictionary<object, HandlerDescriptor> _closed;

        private static readonly IDictionary<CallbackPolicy, CallbackPolicyDescriptor> Empty =
            new ReadOnlyDictionary<CallbackPolicy, CallbackPolicyDescriptor>(
                new Dictionary<CallbackPolicy, CallbackPolicyDescriptor>());

        public HandlerDescriptor(Type handlerType,
            IDictionary<CallbackPolicy, CallbackPolicyDescriptor> policies,
            IDictionary<CallbackPolicy, CallbackPolicyDescriptor> staticPolicies,
            HandlerDescriptorVisitor visitor = null)
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
            {
                _visitor = visitor;
                _closed  = new ConcurrentDictionary<object, HandlerDescriptor>();
            }
        }

        public Type HandlerType { get; }

        public Attribute[] Attributes { get; }

        public bool IsOpenGeneric => HandlerType.IsGenericTypeDefinition;

        public IDictionary<CallbackPolicy, CallbackPolicyDescriptor> Policies { get; }
        public IDictionary<CallbackPolicy, CallbackPolicyDescriptor> StaticPolicies { get; }

        public HandlerDescriptor CloseDescriptor(object key, PolicyMemberBinding binding,
            IHandlerDescriptorFactory factory)
        {
            return _closed.GetOrAdd(key, k =>
            {
                var closedType = binding.CloseHandlerType(HandlerType, k);
                return closedType != null ? factory.RegisterDescriptor(closedType, _visitor) : null;
            });
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

        private string DebuggerDisplay => $"{HandlerType.FullName}";
    }
}
