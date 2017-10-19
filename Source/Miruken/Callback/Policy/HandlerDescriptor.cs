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
        private readonly Dictionary<CallbackPolicy, CallbackPolicyDescriptor> _policies;
        private readonly ConcurrentDictionary<object, Type> _closed;
        private readonly Lazy<Attribute[]> _attributes;
        private readonly Lazy<IFilterProvider[]> _filters;

        public HandlerDescriptor(Type handlerType)
        {
            if (!handlerType.IsClass || handlerType.IsAbstract)
                throw new ArgumentException("Only concrete classes can be handlers");

            HandlerType = handlerType;
            var members = handlerType.FindMembers(Members, Binding, IsCategory, null);
            foreach (var member in members)
            {
                MethodDispatch dispatch = null;
                var method = member as MethodInfo
                          ?? ((PropertyInfo)member).GetMethod;
                var attributes = Attribute.GetCustomAttributes(member, false);

                foreach (var category in attributes.OfType<CategoryAttribute>())
                {
                    var policy = category.CallbackPolicy;
                    var rule   = policy.MatchMethod(method, category);
                    if (rule == null)
                        throw new InvalidOperationException(
                            $"The policy for {category.GetType().FullName} rejected method '{method.GetDescription()}'");

                    dispatch = dispatch ?? new MethodDispatch(method, attributes);
                    var binding = rule.Bind(dispatch, category);

                    if (_policies == null)
                        _policies = new Dictionary<CallbackPolicy, CallbackPolicyDescriptor>();

                    CallbackPolicyDescriptor descriptor;
                    if (!_policies.TryGetValue(policy, out descriptor))
                    {
                        descriptor = new CallbackPolicyDescriptor(policy);
                        _policies.Add(policy, descriptor);
                    }

                    descriptor.Add(binding, handlerType);
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

        public Attribute[] Attributes => _attributes.Value;
        public IFilterProvider[] Filters => _filters.Value;
        public bool IsOpenGeneric => HandlerType.IsGenericTypeDefinition;

        internal bool Dispatch(
            CallbackPolicy policy, object target, 
            object callback, bool greedy, IHandler composer,
            ResultsDelegate results = null)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            CallbackPolicyDescriptor descriptor = null;
            if (_policies?.TryGetValue(policy, out descriptor) != true)
                return false;

            var dispatched = false;
            var key        = policy.GetKey(callback);

            foreach (var method in descriptor.GetInvariantMethods(key))
            {
                dispatched = method.Dispatch(
                    target, callback, composer, results) 
                    || dispatched;
                if (dispatched && !greedy) return true;
            }

            foreach (var method in descriptor.GetCompatibleMethods(key))
            {
                dispatched = method.Item1.Dispatch(
                    target, callback, composer, results) 
                    || dispatched;
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
                return _descriptors.GetOrAdd(type,
                    t => new Lazy<HandlerDescriptor>(() => new HandlerDescriptor(t)))
                    .Value;
            }
            catch
            {
                Lazy<HandlerDescriptor> descriptor;
                _descriptors.TryRemove(type, out descriptor);
                throw;
            }
        }

        public static void ResetDescriptors()
        {
            _descriptors.Clear();
        }

        public static IEnumerable<Type> GetCallbackHandlers(
            CallbackPolicy policy, object callback)
        {
            var key   = policy.GetKey(callback);
            var types = new SortedSet<Tuple<Type, int>>(
                WeightedComparer<Type>.Instance);

            foreach (var descriptor in _descriptors)
            {
                CallbackPolicyDescriptor cpd = null;
                var handler = descriptor.Value.Value;
                if (handler._policies?.TryGetValue(policy, out cpd) == true)
                {
                    var binding = 
                        cpd.GetInvariantMethods(key)
                           .Where(b => b.Approves(callback))
                           .Select(b => Tuple.Create(b, 0))
                           .FirstOrDefault() ??
                        cpd.GetCompatibleMethods(key)
                           .FirstOrDefault(b => b.Item1.Approves(callback));
                    if (binding != null)
                    {
                        if (handler.IsOpenGeneric)
                        {
                            bool added;
                            var handlerType = handler._closed.GetOrAdd(key,
                                k => binding.Item1.CloseHandlerType(handler.HandlerType, k),
                                out added);
                            if (added && handlerType != null)
                                types.Add(Tuple.Create(handlerType, binding.Item2));
                        }
                        else
                            types.Add(Tuple.Create(handler.HandlerType, binding.Item2));
                    }
                }
            }

            return types.Select(type => type.Item1);
        }

        public static IEnumerable<PolicyMethodBinding> GetPolicyMethods(
            CallbackPolicy policy, object key)
        {
            var bindings = new SortedSet<Tuple<PolicyMethodBinding, int>>(
                WeightedComparer<PolicyMethodBinding>.Instance);

            foreach (var descriptor in _descriptors)
            {
                CallbackPolicyDescriptor cpd = null;
                var handler = descriptor.Value.Value;
                if (handler._policies?.TryGetValue(policy, out cpd) == true)
                {
                    foreach (var binding in cpd.GetInvariantMethods(key))
                        bindings.Add(Tuple.Create(binding, 0));
                    foreach (var binding in cpd.GetCompatibleMethods(key))
                        bindings.Add(binding);
                }
            }

            return bindings.Select(binding => binding.Item1);
        }

        public static IEnumerable<PolicyMethodBinding> GetPolicyMethods(CallbackPolicy policy)
        {
            foreach (var descriptor in _descriptors)
            {
                CallbackPolicyDescriptor cpd = null;
                var handler = descriptor.Value.Value;
                if (handler._policies?.TryGetValue(policy, out cpd) == true)
                {
                    foreach (var binding in cpd.GetInvariantMethods())
                        yield return binding;
                }
            }
        }

        public static IEnumerable<PolicyMethodBinding> GetPolicyMethods<T>(CallbackPolicy policy)
        {
            return GetPolicyMethods(policy).Where(m => (m.Key as Type)?.Is<T>() == true);
        }

        private static bool IsCategory(MemberInfo member, object criteria)
        {
            if (member.DeclaringType == typeof(object))
                return false;
            var method = member as MethodInfo;
            if (method != null)
            {
                if (method.IsSpecialName || method.IsFamily)
                    return false;
            }
            else if (!((PropertyInfo)member).CanRead)
                return false; 
            return member.IsDefined(typeof(CategoryAttribute));
        }

        private static readonly ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>
            _descriptors = new ConcurrentDictionary<Type, Lazy<HandlerDescriptor>>();

        private const MemberTypes Members  = MemberTypes.Method 
                                           | MemberTypes.Property;

        private const BindingFlags Binding = BindingFlags.Instance 
                                           | BindingFlags.Public 
                                           | BindingFlags.NonPublic;
    }
}