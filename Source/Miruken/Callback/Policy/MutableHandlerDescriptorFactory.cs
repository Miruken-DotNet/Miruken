namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Bindings;
    using Infrastructure;

    public class MutableHandlerDescriptorFactory : IHandlerDescriptorFactory
    {
        private HandlerDescriptorVisitor _visitor;

        public static MutableHandlerDescriptorFactory Default = new MutableHandlerDescriptorFactory();

        private readonly ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>
            Descriptors = new ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>();

        public MutableHandlerDescriptorFactory()
        {          
        }

        public MutableHandlerDescriptorFactory(HandlerDescriptorVisitor visitor)
        {
            _visitor = visitor;
        }

        public HandlerDescriptor GetDescriptor(Type type)
        {
            try
            {
                return Descriptors.GetOrAdd(type,
                        t => new Lazy<HandlerDescriptor>(() => CreateDescriptor(t)))
                    .Value;
            }
            catch
            {
                Descriptors.TryRemove(type, out _);
                throw;
            }
        }

        public IEnumerable<Type> GetStaticHandlers(CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, false, true);
        }

        public IEnumerable<Type> GetInstanceHandlers(CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, true, false);
        }

        public IEnumerable<Type> GetCallbackHandlers(CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, true, true);
        }

        public IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy)
        {
            return Descriptors.SelectMany(descriptor =>
            {
                CallbackPolicyDescriptor cpd = null;
                var handler = descriptor.Value.Value;
                var staticMembers = handler.StaticPolicies?.TryGetValue(policy, out cpd) == true
                    ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                var members = handler.Policies?.TryGetValue(policy, out cpd) == true
                    ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                if (staticMembers == null) return members;
                return members == null ? staticMembers : staticMembers.Concat(members);
            });
        }

        private IEnumerable<Type> GetCallbackHandlers(
            CallbackPolicy policy, object callback, bool instance, bool @static)
        {
            if (Descriptors.Count == 0)
                return Enumerable.Empty<Type>();

            List<PolicyMemberBinding> invariants = null;
            List<PolicyMemberBinding> compatible = null;

            foreach (var descriptor in Descriptors)
            {
                var handler = descriptor.Value.Value;
                CallbackPolicyDescriptor instanceCallbacks = null;
                if (instance)
                    handler.Policies?.TryGetValue(policy, out instanceCallbacks);

                var binding = instanceCallbacks?.GetInvariantMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (invariants == null)
                        invariants = new List<PolicyMemberBinding>();
                    invariants.Add(binding);
                    continue;
                }

                CallbackPolicyDescriptor staticCallbacks = null;
                if (@static)
                    handler.StaticPolicies?.TryGetValue(policy, out staticCallbacks);
                binding = staticCallbacks?.GetInvariantMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (invariants == null)
                        invariants = new List<PolicyMemberBinding>();
                    invariants.Add(binding);
                    continue;
                }

                binding = instanceCallbacks?.GetCompatibleMembers(callback).FirstOrDefault()
                       ?? staticCallbacks?.GetCompatibleMembers(callback).FirstOrDefault();
                if (binding != null)
                {
                    if (compatible == null)
                        compatible = new List<PolicyMemberBinding>();
                    compatible.AddSorted(binding, policy);
                }
            }

            if (invariants == null && compatible == null)
                return Enumerable.Empty<Type>();

            var bindings = invariants == null ? compatible
                : compatible == null ? invariants
                : invariants.Concat(compatible);

            return bindings.Select(binding =>
            {
                var handler = binding.Dispatcher.Owner;
                if (handler.IsOpenGeneric)
                {
                    var key = policy.GetKey(callback);
                    return handler.CloseType(key, binding);
                }
                return handler.HandlerType;
            })
            .Where(type => type != null)
            .Distinct();
        }

        private HandlerDescriptor CreateDescriptor(Type handlerType)
        {
            IDictionary<CallbackPolicy, List<PolicyMemberBinding>> instancePolicies = null;
            IDictionary<CallbackPolicy, List<PolicyMemberBinding>> staticPolicies   = null;

            var members = handlerType.FindMembers(Members, Binding, IsCategory, null);

            foreach (var member in members)
            {
                MethodInfo          method              = null;
                MethodDispatch      methodDispatch      = null;
                ConstructorInfo     constructor         = null;
                ConstructorDispatch constructorDispatch = null;

                switch (member)
                {
                    case MethodInfo mi:
                        method = mi;
                        break;
                    case PropertyInfo pi:
                        method = pi.GetMethod;
                        break;
                    case ConstructorInfo ci:
                        constructor = ci;
                        break;
                    default:
                        continue;
                }

                var attributes = Attribute.GetCustomAttributes(member, false);

                foreach (var category in attributes.OfType<CategoryAttribute>())
                {
                    PolicyMemberBinding memberBinding;
                    var policy = category.CallbackPolicy;

                    if (constructor != null)
                    {
                        constructorDispatch = constructorDispatch
                                            ?? new ConstructorDispatch(constructor, attributes);
                        memberBinding = new PolicyMemberBinding(policy,
                            new PolicyMemberBindingInfo(null, constructorDispatch, category)
                            {
                                OutKey = constructor.ReflectedType
                            });
                        if (handlerType.Is<IInitialize>())
                            memberBinding.AddFilters(InitializeProvider.Instance);
                    }
                    else
                    {
                        var rule = policy.MatchMethod(method, category);
                        if (rule == null)
                            throw new InvalidOperationException(
                                $"The policy for {category.GetType().FullName} rejected method '{method.GetDescription()}'");

                        methodDispatch = methodDispatch ?? new MethodDispatch(method, attributes);
                        memberBinding  = rule.Bind(methodDispatch, category);
                    }

                    var policies = constructor != null || method.IsStatic
                        ? staticPolicies ?? (staticPolicies =
                              new Dictionary<CallbackPolicy, List<PolicyMemberBinding>>())
                        : instancePolicies ?? (instancePolicies =
                              new Dictionary<CallbackPolicy, List<PolicyMemberBinding>>());

                    if (!policies.TryGetValue(policy, out var bindings))
                    {
                        bindings = new List<PolicyMemberBinding>();
                        policies.Add(policy, bindings);
                    }

                    bindings.Add(memberBinding);
                }
            }

            var descriptor = new HandlerDescriptor(handlerType,
                instancePolicies?.ToDictionary(p => p.Key,
                    p => new CallbackPolicyDescriptor(p.Key, p.Value)),
                staticPolicies?.ToDictionary(p => p.Key,
                    p => new CallbackPolicyDescriptor(p.Key, p.Value)));

            if (_visitor != null)
            {
                if (instancePolicies != null)
                {
                    foreach (var binding in instancePolicies.SelectMany(p => p.Value))
                        _visitor(descriptor, binding);
                }

                if (staticPolicies != null)
                {
                    foreach (var binding in staticPolicies.SelectMany(p => p.Value))
                        _visitor(descriptor, binding);
                }
            }

            return descriptor;
        }

        private static bool IsCategory(MemberInfo member, object criteria)
        {
            if (member.DeclaringType == typeof(object) ||
                member.DeclaringType == typeof(MarshalByRefObject))
                return false;
            switch (member)
            {
                case MethodInfo method when
                    method.IsSpecialName || method.IsFamily:
                    return false;
                case PropertyInfo property when !property.CanRead:
                    return false;
            }
            return member.IsDefined(typeof(CategoryAttribute));
        }

        private const MemberTypes Members = MemberTypes.Method
                                          | MemberTypes.Property
                                          | MemberTypes.Constructor;

        private const BindingFlags Binding = BindingFlags.Instance
                                           | BindingFlags.Public
                                           | BindingFlags.Static
                                           | BindingFlags.NonPublic;
    }
}

