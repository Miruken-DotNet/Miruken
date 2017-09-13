﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public abstract class CovariantPolicy : CallbackPolicy, IComparer<Type>
    {
        protected static bool AcceptKey(Type type, Type key)
        {
            if (type.IsGenericTypeDefinition)
                return key.GetOpenTypeConformance(type) != null;
            if (key.IsGenericTypeDefinition)
                return type.IsGenericType && 
                    type.GetGenericTypeDefinition() == key;
            return type.IsAssignableFrom(key);
        }

        int IComparer<Type>.Compare(Type x, Type y)
        {
            if (x == y) return 0;
            return y == null || AcceptKey(x, y) ? -1 : 1;
        }

        public static CovariantPolicy<Cb> Create<Cb>(
            Func<Cb, object> key,
            Action<CovariantPolicyBuilder<Cb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new CovariantPolicy<Cb>(key);
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

        public override object GetKey(object callback)
        {
            return callback is Cb ? Key((Cb)callback) : null;
        }

        public override IEnumerable GetCompatibleKeys(object key, IEnumerable output)
        {
            var type = key as Type;
            if (type == null)
                return output.Cast<object>().Where(k => 
                    !Equals(key, k) && Equals(k, key));
            return type == typeof(object) 
                 ? output.Cast<object>().Where(t => (t as Type)?.IsGenericTypeDefinition != true)
                 : output.OfType<Type>().Where(t => t != type && AcceptKey(type, t))
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

        public CallbackArgument<Cb> Callback  => CallbackArgument<Cb>.Instance;
        public ReturnsKey           ReturnKey => ReturnsKey.Instance;

        public ExtractArgument<Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Cb, Res>(extract);
        }

        public CovariantPolicyBuilder<Cb> MatchMethodWithCallback(params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<Cb>))
                MatchMethod(args.Concat(new [] { Callback }).ToArray());
            MatchMethod(args);
            return this;
        }

        public CovariantPolicyBuilder<Cb> MatchMethodWithCallback(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<Cb>))
                MatchMethod(returnRule, args.Concat(new[] { Callback }).ToArray());
            MatchMethod(returnRule, args);
            return this;
        }
    }
}
