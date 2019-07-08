namespace Miruken.Register
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Callback;
    using Callback.Policy;
    using Callback.Policy.Bindings;
    using Infrastructure;
    using Microsoft.Extensions.DependencyInjection;

    public class ImmutableHandlerDescriptorFactory : IHandlerDescriptorFactory
    {
        private readonly HandlerDescriptorVisitor _visitor;

        private readonly IDictionary<Type, HandlerDescriptor>
            Descriptors = new Dictionary<Type, HandlerDescriptor>();

        private readonly ConcurrentDictionary<Tuple<object, CallbackPolicy, bool, bool>,
            IEnumerable<HandlerDescriptor>> HandlerCache = 
            new ConcurrentDictionary<Tuple<object, CallbackPolicy, bool, bool>, IEnumerable<HandlerDescriptor>>();

        private static readonly Provides[] ImplicitProvides = { new Provides() };

        public ImmutableHandlerDescriptorFactory(
            IServiceCollection services,
            HandlerDescriptorVisitor visitor = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            _visitor = visitor;

            foreach (var descriptor in services)
            {
                var implementationType = GetImplementationType(descriptor);
                if (implementationType != null)
                {
                    var handler = CreateDescriptor(
                        implementationType, ServiceConfiguration.For(descriptor));
                    if (handler != null)
                        Descriptors.Add(implementationType, handler);
                }
            }
        }

        public HandlerDescriptor GetDescriptor(Type type)
        {
            return Descriptors.TryGetValue(type, out var descriptor)
                ? descriptor
                : null;
        }

        public HandlerDescriptor RegisterDescriptor(Type type,
            HandlerDescriptorVisitor visitor = null)
        {
            throw new NotSupportedException(
                "This factory is immutable and does not support ad-hoc registrations");
        }

        public IEnumerable<HandlerDescriptor> GetStaticHandlers(CallbackPolicy policy, object callback)
        {
            return GetCachedCallbackHandlers(policy, callback, false, true);
        }

        public IEnumerable<HandlerDescriptor> GetInstanceHandlers(CallbackPolicy policy, object callback)
        {
            return GetCachedCallbackHandlers(policy, callback, true, false);
        }

        public IEnumerable<HandlerDescriptor> GetCallbackHandlers(CallbackPolicy policy, object callback)
        {
            return GetCachedCallbackHandlers(policy, callback, true, true);
        }

        public IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy)
        {
            return Descriptors.SelectMany(descriptor =>
            {
                CallbackPolicyDescriptor cpd = null;
                var handler = descriptor.Value;
                var staticMembers = handler.StaticPolicies?.TryGetValue(policy, out cpd) == true
                    ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                var members = handler.Policies?.TryGetValue(policy, out cpd) == true
                    ? cpd.InvariantMembers : Enumerable.Empty<PolicyMemberBinding>();
                if (staticMembers == null) return members;
                return members == null ? staticMembers : staticMembers.Concat(members);
            });
        }

        private IEnumerable<HandlerDescriptor> GetCachedCallbackHandlers(
            CallbackPolicy policy, object callback, bool instance, bool @static)
        {
            var key = Tuple.Create(policy.GetKey(callback), policy, instance, @static);
            return HandlerCache.GetOrAdd(key, k =>
                GetCallbackHandlers(policy, callback, instance, @static));
        }

        private IEnumerable<HandlerDescriptor> GetCallbackHandlers(
            CallbackPolicy policy, object callback, bool instance, bool @static)
        {
            if (Descriptors.Count == 0)
                return Enumerable.Empty<HandlerDescriptor>();

            List<PolicyMemberBinding> invariants = null;
            List<PolicyMemberBinding> compatible = null;

            foreach (var descriptor in Descriptors)
            {
                var handler = descriptor.Value;
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
                return Enumerable.Empty<HandlerDescriptor>();

            var bindings = invariants == null ? compatible
                : compatible == null ? invariants
                : invariants.Concat(compatible);

            return bindings.Select(binding =>
            {
                var handler = binding.Dispatcher.Owner;
                if (handler.IsOpenGeneric)
                {
                    var key = policy.GetKey(callback);
                    return handler.CloseDescriptor(key, binding, this);
                }
                return handler;
            })
            .Where(descriptor => descriptor != null)
            .Distinct();
        }

        private HandlerDescriptor CreateDescriptor(Type handlerType,
            HandlerDescriptorVisitor visitor = null)
        {
            IDictionary<CallbackPolicy, List<PolicyMemberBinding>> instancePolicies = null;
            IDictionary<CallbackPolicy, List<PolicyMemberBinding>> staticPolicies = null;

            var members = handlerType.FindMembers(Members, Binding, IsCategory, null);

            foreach (var member in members)
            {
                MethodInfo method = null;
                MethodDispatch methodDispatch = null;
                ConstructorInfo constructor = null;
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
                var categories = attributes.OfType<CategoryAttribute>().ToArray();

                var provideImplicit = categories.Length == 0
                                   && constructor?.IsPublic == true
                                   && !handlerType.IsAbstract
                                   && !handlerType.IsDefined(typeof(UnmanagedAttribute), true);

                if (provideImplicit)
                    categories = ImplicitProvides;

                foreach (var category in categories)
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
                        memberBinding = rule.Bind(methodDispatch, category);
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

            if (instancePolicies == null && staticPolicies == null)
                return null;

            var descriptor = new HandlerDescriptor(handlerType,
                instancePolicies?.ToDictionary(p => p.Key,
                    p => new CallbackPolicyDescriptor(p.Key, p.Value)),
                staticPolicies?.ToDictionary(p => p.Key,
                    p => new CallbackPolicyDescriptor(p.Key, p.Value)));

            visitor = _visitor + visitor;

            if (visitor != null)
            {
                if (instancePolicies != null)
                {
                    foreach (var binding in instancePolicies.SelectMany(p => p.Value))
                        visitor(descriptor, binding);
                }

                if (staticPolicies != null)
                {
                    foreach (var binding in staticPolicies.SelectMany(p => p.Value))
                        visitor(descriptor, binding);
                }
            }

            return descriptor;
        }

        private static Type GetImplementationType(ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationType != null)
                return descriptor.ImplementationType;

            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance.GetType();

            if (descriptor.ImplementationFactory != null)
            {
                var typeArguments = descriptor.ImplementationFactory
                    .GetType().GenericTypeArguments;
                if (typeArguments.Length == 2)
                    return typeArguments[1];
            }

            return null;
        }

        private static bool IsCategory(MemberInfo member, object criteria)
        {
            if (member.DeclaringType == typeof(object) ||
                member.DeclaringType == typeof(MarshalByRefObject))
                return false;
            switch (member)
            {
                case ConstructorInfo _:
                    return true;
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
