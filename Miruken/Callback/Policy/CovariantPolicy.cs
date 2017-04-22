﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class CovariantPolicy : CallbackPolicy, IComparer<Type>
    {
        public MethodBinding Bind(
            MethodRule rule, MethodDispatch dispatch,
            DefinitionAttribute attribute)
        {
            var binding = Binder?.Invoke(rule, dispatch, this, attribute)
                ?? new MethodBinding(rule, dispatch, this, attribute);
            InferVariance(binding);
            return binding;
        }

        protected static void InferVariance(MethodBinding method)
        {
            var key = method.Attribute.Key;
            var restrict = key as Type;
            if (restrict != null)
            {
                if (method.VarianceType == null ||
                    method.VarianceType.IsAssignableFrom(restrict))
                    method.VarianceType = restrict;
            }
        }

        protected static bool AcceptKey(Type type, Type key)
        {
            if (key.IsGenericTypeDefinition)
            {
                if (type.IsGenericType)
                {
                    if (key.IsInterface)
                        return type.GetInterface(key.FullName) != null;
                    while (type != typeof(object) &&
                           type?.IsGenericType == true)
                    {
                        if (type.GetGenericTypeDefinition() == key)
                            return true;
                        type = type.BaseType;
                    }
                }
                return false;
            }
            return type.IsAssignableFrom(key);
        }

        int IComparer<Type>.Compare(Type x, Type y)
        {
            if (x == y) return 0;
            return y == null || AcceptKey(x, y) ? -1 : 1;
        }

        public static CovariantPolicy<Cb> Create<Cb>(
            Func<Cb, object> target,
            Action<CovariantPolicyBuilder<Cb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy = new CovariantPolicy<Cb>(target);
            var builder = new CovariantPolicyBuilder<Cb>(policy);
            build(builder);
            return policy;
        }
    }

    public class CovariantPolicy<Cb> : CovariantPolicy
    {
        public CovariantPolicy(Func<Cb, object> key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Key        = key;
            ResultType = GetResultType;
        }

        public Func<Cb, object> Key { get; }

        public override IEnumerable SelectKeys(object callback, ICollection keys)
        {
            if (!(callback is Cb))
                return Enumerable.Empty<object>();
            var key  = Key((Cb)callback);
            var type = key as Type;
            if (type == null)
                return Enumerable.Repeat(key, 1);
            var typeKeys = keys.OfType<Type>();
            return type == typeof(object) 
                 ? typeKeys.Where(k => !k.IsGenericTypeDefinition)
                 : typeKeys.Where(k => AcceptKey(type, k))
                      .OrderBy(t => t, this);
        }

        private Type GetResultType(object callback)
        {
            return Key((Cb)callback) as Type;
        }
    }

    public class CovariantPolicyBuilder<Cb>
        : CallbackPolicyBuilder<CovariantPolicy<Cb>, CovariantPolicyBuilder<Cb>>
    {
        public CovariantPolicyBuilder(CovariantPolicy<Cb> policy)
            : base(policy)
        {
        }

        public CallbackArgument<Cb> Callback => CallbackArgument<Cb>.Instance;
        public ReturnsKey           Return   => ReturnsKey.Instance;

        public ExtractArgument<Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Cb, Res>(extract);
        }

        public CovariantPolicyBuilder<Cb> MatchMethod(params ArgumentRule[] args)
        {
            Policy.AddMethodRule(new MethodRule(Policy.Bind, args));
            return this;
        }

        public CovariantPolicyBuilder<Cb> MatchMethod(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            Policy.AddMethodRule(new MethodRule(Policy.Bind, returnRule, args));
            return this;
        }
    }
}
