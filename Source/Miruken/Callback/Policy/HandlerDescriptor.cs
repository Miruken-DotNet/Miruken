namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;
    
    public class HandlerDescriptor
    {
        private readonly ConcurrentDictionary<object, Type> _closed;
        private readonly Dictionary<CallbackPolicy, CallbackPolicyDescriptor> _policies;
        private readonly Dictionary<CallbackPolicy, CallbackPolicyDescriptor> _staticPolicies;
        private readonly Lazy<IFilterProvider[]> _filters;
        private readonly Lazy<Attribute[]> _attributes;

        private static readonly ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>
            Descriptors = new ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>();

        public HandlerDescriptor(Type handlerType)
        {
            if (!handlerType.IsClass)
                throw new ArgumentException("Only classes can be handlers");

            HandlerType = handlerType;
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
                        ? _staticPolicies ?? (_staticPolicies = 
                              new Dictionary<CallbackPolicy, CallbackPolicyDescriptor>())
                        : _policies ?? (_policies =
                              new Dictionary<CallbackPolicy, CallbackPolicyDescriptor>());

                    if (!policies.TryGetValue(policy, out var descriptor))
                    {
                        descriptor = new CallbackPolicyDescriptor(policy);
                        policies.Add(policy, descriptor);
                    }

                    descriptor.Add(memberBinding);
                }
            }

            _attributes = new Lazy<Attribute[]>(() => 
                Attribute.GetCustomAttributes(handlerType, true).Normalize());

            _filters = new Lazy<IFilterProvider[]>(() =>
                Attributes.OfType<IFilterProvider>().ToArray().Normalize());

            if (handlerType.IsGenericTypeDefinition)
                _closed = new ConcurrentDictionary<object, Type>();
        }

        public Type HandlerType { get; }

        public Attribute[]       Attributes => _attributes.Value;
        public IFilterProvider[] Filters    => _filters.Value;

        public bool IsOpenGeneric => HandlerType.IsGenericTypeDefinition;

        internal bool Dispatch(
            CallbackPolicy policy, object target, 
            object callback, bool greedy, IHandler composer,
            ResultsDelegate results = null)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            CallbackPolicyDescriptor descriptor = null;
            var policies = target == null || target is Type 
                ? _staticPolicies : _policies;
            if (policies?.TryGetValue(policy, out descriptor) != true)
                return false;

            var dispatched = false;
            foreach (var method in descriptor.GetInvariantMethods(callback))
            {
                dispatched = method.Dispatch(
                    target, callback, composer, results) || dispatched;
                if (dispatched && !greedy) return true;
            }

            foreach (var method in descriptor.GetCompatibleMethods(callback))
            {
                dispatched = method.Dispatch(
                    target, callback, composer, results) || dispatched;
                if (dispatched && !greedy) return true;
            }

            return dispatched;
        }

        public static HandlerDescriptor GetDescriptor<T>()
        {
            return GetDescriptor(typeof(T));
        }

        public static HandlerDescriptor GetDescriptor(Type type)
        {
            try
            {
                return Descriptors.GetOrAdd(type,
                    t => new Lazy<HandlerDescriptor>(() => new HandlerDescriptor(t)))
                    .Value;
            }
            catch
            {
                Descriptors.TryRemove(type, out _);
                throw;
            }
        }

        public static void ResetDescriptors()
        {
            Descriptors.Clear();
        }

        public static IEnumerable<Type> GetStaticHandlers(
            CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, false, true);
        }

        public static IEnumerable<Type> GetInstanceHandlers(
            CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, true, false);
        }

        public static IEnumerable<Type> GetCallbackHandlers(
            CallbackPolicy policy, object callback)
        {
            return GetCallbackHandlers(policy, callback, true, true);
        }

        private static IEnumerable<Type> GetCallbackHandlers(
            CallbackPolicy policy, object callback, bool instance, bool @static)
        {
            return Descriptors.Select(descriptor =>
            {
                CallbackPolicyDescriptor cpd = null;
                var handler = descriptor.Value.Value;
                if (@static)
                {
                    var binding =
                        handler._staticPolicies?.TryGetValue(policy, out cpd) == true
                        ? cpd.GetInvariantMethods(callback).FirstOrDefault() ??
                          cpd.GetCompatibleMethods(callback).FirstOrDefault()
                        : null;
                    if (binding != null) return binding;
                }
                if (instance)
                {
                    return handler._policies?.TryGetValue(policy, out cpd) == true
                         ? cpd.GetInvariantMethods(callback).FirstOrDefault() ??
                           cpd.GetCompatibleMethods(callback).FirstOrDefault()
                         : null;
                }
                return null;
            })
            .Where(binding => binding != null)
            .OrderBy(binding => binding.Key, policy)
            .Select(binding =>
            {
                var handler = binding.Dispatcher.Owner;
                if (handler.IsOpenGeneric)
                {
                    var key = policy.GetKey(callback);
                    return handler._closed.GetOrAdd(key,
                        k => binding.CloseHandlerType(handler.HandlerType, k));
                }
                return handler.HandlerType;
            })
            .Where(type => type != null)
            .Distinct();
        }

        public static IEnumerable<PolicyMemberBinding>
            GetPolicyMethods(CallbackPolicy policy)
        {
            return Descriptors.SelectMany(descriptor =>
            {
                CallbackPolicyDescriptor cpd = null;
                var handler  = descriptor.Value.Value;
                var smethods = handler._staticPolicies?.TryGetValue(policy, out cpd) == true
                     ? cpd.GetInvariantMethods()
                     : Enumerable.Empty<PolicyMemberBinding>();
                var methods  = handler._policies?.TryGetValue(policy, out cpd) == true
                     ? cpd.GetInvariantMethods()
                     : Enumerable.Empty<PolicyMemberBinding>();
                if (smethods == null) return methods;
                return methods == null ? smethods : smethods.Concat(methods);
            });
        }

        public static IEnumerable<PolicyMemberBinding> 
            GetPolicyMethods<T>(CallbackPolicy policy)
        {
            return GetPolicyMethods(policy)
                .Where(m => (m.Key as Type)?.Is<T>() == true);
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

        private const MemberTypes Members  = MemberTypes.Method 
                                           | MemberTypes.Property
                                           | MemberTypes.Constructor;

        private const BindingFlags Binding = BindingFlags.Instance 
                                           | BindingFlags.Public
                                           | BindingFlags.Static
                                           | BindingFlags.NonPublic;
    }
}