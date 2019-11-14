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
        private readonly Func<Type, HandlerDescriptorVisitor, int?, HandlerDescriptor> _factory;
        private readonly HandlerDescriptorVisitor _visitor;
        private readonly ConcurrentDictionary<Type, HandlerDescriptor> _closed;
        private readonly ConcurrentDictionary<object, Type> _keyTypes;

        private static readonly IDictionary<CallbackPolicy, CallbackPolicyDescriptor> Empty =
            new ReadOnlyDictionary<CallbackPolicy, CallbackPolicyDescriptor>(
                new Dictionary<CallbackPolicy, CallbackPolicyDescriptor>());

        public HandlerDescriptor(Type handlerType,
            IDictionary<CallbackPolicy, CallbackPolicyDescriptor> policies,
            IDictionary<CallbackPolicy, CallbackPolicyDescriptor> staticPolicies,
            Func<Type, HandlerDescriptorVisitor, int?, HandlerDescriptor> factory = null,
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

            if (IsOpenGeneric)
            {
                _factory  = factory;
                _visitor  = visitor;
                _closed   = new ConcurrentDictionary<Type, HandlerDescriptor>();
                _keyTypes = new ConcurrentDictionary<object, Type>();
            }
        }

        public Type HandlerType { get; }

        public Attribute[] Attributes { get; }

        public int? Priority { get; set; }

        public bool IsOpenGeneric => HandlerType.IsGenericTypeDefinition;

        public IDictionary<CallbackPolicy, CallbackPolicyDescriptor> Policies       { get; }
        public IDictionary<CallbackPolicy, CallbackPolicyDescriptor> StaticPolicies { get; }

        public HandlerDescriptor CloseDescriptor(Type closedType)
        {
            if (_factory == null)
                throw new InvalidOperationException($"{HandlerType.FullName} does not represent an open type");

            if (!closedType.IsGenericType || closedType.GetGenericTypeDefinition() != HandlerType)
                throw new InvalidOperationException($"{closedType.FullName} is not closed on {HandlerType.FullName}");

            return _closed.GetOrAdd(closedType, k => _factory(closedType, _visitor, Priority));
        }

        private HandlerDescriptor CloseDescriptor(object key, PolicyMemberBinding binding)
        {
            var closedType = _keyTypes.GetOrAdd(key, k => binding.CloseHandlerType(HandlerType, k));
            return closedType != null ? CloseDescriptor(closedType) : null;
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

            var dispatched     = false;
            var hasConstructor = false;
            foreach (var member in descriptor.GetInvariantMembers(callback))
            {
                var isConstructor = member.Dispatcher.IsConstructor;
                if (isConstructor && hasConstructor) continue;
                if (_factory != null)
                    return ClosedDispatch(member, policy, target, callback, greedy, composer, results);
                dispatched = member.Dispatch(target, callback, composer, results) || dispatched;
                if (dispatched)
                {
                    if (!greedy) return true;
                    hasConstructor |= isConstructor;
                }
            }

            foreach (var member in descriptor.GetCompatibleMembers(callback))
            {
                var isConstructor = member.Dispatcher.IsConstructor;
                if (isConstructor && hasConstructor) continue;
                if (_factory != null)
                    return ClosedDispatch(member, policy, target, callback, greedy, composer, results);
                dispatched = member.Dispatch( target, callback, composer, results) || dispatched;
                if (dispatched)
                {
                    if (!greedy) return true;
                    hasConstructor |= isConstructor;
                }
            }

            return dispatched;
        }

        private bool ClosedDispatch(
            PolicyMemberBinding member,
            CallbackPolicy policy, object target,
            object callback, bool greedy, IHandler composer,
            ResultsDelegate results = null)
        {
            var closed = CloseDescriptor(policy.GetKey(callback), member);
            return closed?.Dispatch(policy, target, callback, greedy, composer, results)
                   ?? false;
        }

        private string DebuggerDisplay => $"{HandlerType.FullName}";
    }
}
