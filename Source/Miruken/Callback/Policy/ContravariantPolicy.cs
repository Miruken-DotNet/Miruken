﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Bindings;
    using Infrastructure;
    using Rules;

    public class ContravariantPolicy : CallbackPolicy
    {
        public ContravariantPolicy()
        {
            AcceptResult = VerifyResult;
        }

        public override object GetKey(object callback)
        {
            return callback?.GetType();
        }

        public override IEnumerable<object> GetCompatibleKeys(
            object key, IEnumerable available)
        {
            return CompatibleTypes(key as Type, available);
        }

        protected IEnumerable<object> CompatibleTypes(
            Type type, IEnumerable types)
        {
            if (type == null || type == typeof(object))
                return Enumerable.Empty<object>();
            return types.OfType<Type>()
                .Where(t => t != type && IsCompatible(type, t));
        }

        public override int Compare(object key1, object key2)
        {
            if (key1 == key2) return 0;
            var type1 = key1 as Type;
            if (type1 == null) return 1;
            var type2 = key2 as Type;
            if (type2 == null) return -1;
            if (type2.ContainsGenericParameters)
                return type1.ContainsGenericParameters
                     ? type2.GetGenericArguments().Length -
                       type1.GetGenericArguments().Length
                     : -1;
            if (type1.ContainsGenericParameters) return 1;
            return type2.IsAssignableFrom(type1) ? -1 : 1;
        }

        private static bool VerifyResult(object result, MemberBinding binding)
        {
            return result != null || binding.Dispatcher.IsVoid;
        }

        private static bool IsCompatible(Type type, Type key)
        {
            if (type == key) return true;
            if (type == null || key == null) return false;
            if (key.IsAssignableFrom(type)) return true;
            return type.GetOpenTypeConformance(key) != null;
        }

        public static ContravariantPolicy Create(
            Action<ContravariantPolicyBuilder> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new ContravariantPolicy();
            var builder = new ContravariantPolicyBuilder(policy);
            build(builder);
            return policy;
        }

        public static ContravariantPolicy<TCb> Create<TCb>(
            Func<TCb, object> target,
            Action<ContravariantPolicyBuilder<TCb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new ContravariantPolicy<TCb>(target);
            var builder = new ContravariantPolicyBuilder<TCb>(policy);
            build(builder);
            return policy;
        }
    }

    public class ContravariantPolicy<TCb> : ContravariantPolicy
    {
        public ContravariantPolicy(Func<TCb, object> target)
        {
            Target = target 
                ?? throw new ArgumentNullException(nameof(target));
        }

        public Func<TCb, object> Target { get; }

        public override object GetKey(object callback)
        {
            return (callback as ICallbackKey)?.Key
                ?? (callback is TCb cb ? GetTargetType(cb)
                   : callback?.GetType());
        }

        public override IEnumerable<object> GetCompatibleKeys(
            object key, IEnumerable available)
        {
            return CompatibleTypes(key as Type, available);
        }

        private Type GetTargetType(TCb callback)
        {
            var target = Target(callback);
            return target as Type ?? target?.GetType();
        }
    }

    public class ContravariantPolicyBuilder
        : CallbackPolicyBuilder<ContravariantPolicy, ContravariantPolicyBuilder>
    {
        public ContravariantPolicyBuilder(ContravariantPolicy policy)
            : base(policy)
        {
        }

        public CallbackArgument Callback => CallbackArgument.Instance;

        public ContravariantPolicyBuilder MatchMethodWithCallback(params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument))
                MatchMethod(args.Concat(new [] { Callback }).ToArray());
            MatchMethod(args);
            return this;
        }

        public ContravariantPolicyBuilder MatchMethodWithCallback(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument))
                MatchMethod(returnRule, args.Concat(new[] { Callback }).ToArray());
            MatchMethod(returnRule, args);
            return this;
        }
    }

    public class ContravariantPolicyBuilder<TCb>
         : CallbackPolicyBuilder<ContravariantPolicy<TCb>, ContravariantPolicyBuilder<TCb>>
    {
        public ContravariantPolicyBuilder(ContravariantPolicy<TCb> policy)
            : base(policy)
        {
        }

        public CallbackArgument<TCb> Callback => CallbackArgument<TCb>.Instance;
        public TargetArgument<TCb>   Target   => new(Policy.Target);

        public ExtractArgument<TCb, TRes> Extract<TRes>(Func<TCb, TRes> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<TCb, TRes>(extract);
        }

        public ContravariantPolicyBuilder<TCb> MatchCallbackMethod(params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<TCb>))
                MatchMethod(args.Concat(new[] { Callback }).ToArray());
            MatchMethod(args);
            return this;
        }

        public ContravariantPolicyBuilder<TCb> MatchCallbackMethod(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<TCb>))
                MatchMethod(returnRule, args.Concat(new[] { Callback }).ToArray());
            MatchMethod(returnRule, args);
            return this;
        }
    }
}
