namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Bindings;
    using Infrastructure;

    public abstract class AbstractHandlerDescriptorFactory : IHandlerDescriptorFactory
    {
        private readonly HandlerDescriptorVisitor _visitor;

        private static readonly Provides[] ImplicitProvides = { new Provides() };

        protected AbstractHandlerDescriptorFactory(HandlerDescriptorVisitor visitor = null)
        {
            _visitor = visitor;
        }

        public LifestyleAttribute ImplicitLifestyle { get; set; }

        public abstract HandlerDescriptor GetDescriptor(Type type);

        public abstract HandlerDescriptor RegisterDescriptor(Type type,
            HandlerDescriptorVisitor visitor = null, int? priority = null);

        public IEnumerable<HandlerDescriptor> GetStaticHandlers(CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, false, true);
        }

        public IEnumerable<HandlerDescriptor> GetInstanceHandlers(CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, true, false);
        }

        public IEnumerable<HandlerDescriptor> GetCallbackHandlers(CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, true, true);
        }

        protected abstract IEnumerable<HandlerDescriptor> GetCallbackHandlers(
            CallbackPolicy policy, object callback, bool instance, bool @static);

        public abstract IEnumerable<PolicyMemberBinding> GetPolicyMembers(CallbackPolicy policy);

        protected virtual HandlerDescriptor CreateDescriptor(Type handlerType,
            HandlerDescriptorVisitor visitor = null, int? priority = null)
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

            if (instancePolicies == null && staticPolicies == null)
                return null;

            var callbacks = instancePolicies?.ToDictionary(p => p.Key,
                p => new CallbackPolicyDescriptor(p.Key, p.Value));
            var staticCallbacks = staticPolicies?.ToDictionary(p => p.Key,
                p => new CallbackPolicyDescriptor(p.Key, p.Value));

            var descriptor = handlerType.IsGenericTypeDefinition
                ? new GenericHandlerDescriptor(handlerType, callbacks, staticCallbacks, visitor)
                : new HandlerDescriptor(handlerType, callbacks, staticCallbacks);

            descriptor.Priority = priority;

            visitor = _visitor + visitor;

            if (visitor != null)
            {
                if (instancePolicies != null)
                {
                    foreach (var binding in instancePolicies.SelectMany(p => p.Value))
                        visitor(descriptor, binding);
                }
            }

            if (staticPolicies != null)
            {
                foreach (var binding in staticPolicies.SelectMany(p => p.Value))
                {
                    visitor?.Invoke(descriptor, binding);
                    AddImplicitLifestyle(binding);
                }
            }

            return descriptor;
        }

        private void AddImplicitLifestyle(PolicyMemberBinding binding)
        {
            if (ImplicitLifestyle != null &&
                ReferenceEquals(binding.Category, ImplicitProvides[0]) &&
                !binding.Filters.OfType<LifestyleAttribute>().Any())
            {
                binding.AddFilters(ImplicitLifestyle);
            }
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
