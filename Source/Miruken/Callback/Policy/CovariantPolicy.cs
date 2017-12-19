namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public abstract class CovariantPolicy : CallbackPolicy
    {
        public static CovariantPolicy<TCb> Create<TCb>(
            Func<TCb, object> key,
            Action<CovariantPolicyBuilder<TCb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new CovariantPolicy<TCb>(key);
            var builder = new CovariantPolicyBuilder<TCb>(policy);
            build(builder);
            return policy;
        }

        public override int Compare(object key1, object key2)
        {
            if (key1 == key2) return 0;
            var type1 = key1 as Type;
            if (type1 == null) return 1;
            var type2 = key2 as Type;
            if (type2 == null) return -1;
            if (type2.ContainsGenericParameters) return -1;
            if (type1.ContainsGenericParameters) return 1;
            return type1.IsAssignableFrom(type2) ? -1 : 1;
        }

        protected static bool IsCompatible(Type type, Type key)
        {
            if (type == key) return true;
            if (type == null || key == null) return false;
            if (type.IsGenericTypeDefinition)
                return key.GetOpenTypeConformance(type) != null;
            if (key.IsGenericTypeDefinition)
                return type.IsGenericType && type.GetGenericTypeDefinition() == key;
            return key.IsGenericParameter 
                 ? key.SatisfiesGenericParameterConstraints(type)
                 : type.IsAssignableFrom(key);
        }
    }

    public class CovariantPolicy<TCb> : CovariantPolicy
    {
        public CovariantPolicy(Func<TCb, object> key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            ResultType = GetResultType;
        }

        public Func<TCb, object> Key { get; }

        public override object GetKey(object callback)
        {
            return callback is TCb cb ? Key(cb) : null;
        }

        public override IEnumerable<object> GetCompatibleKeys(
            object key, IEnumerable output)
        {
            var type = key as Type;
            if (type == null)
                return output.Cast<object>()
                    .Where(k => !Equals(key, k) && Equals(k, key));

            if (type == typeof(object))
                return output.OfType<Type>()
                    .Where(t => !t.ContainsGenericParameters);

            return output.OfType<Type>()
                .Where(t => t != type && IsCompatible(type, t));
        }

        private Type GetResultType(object callback)
        {
            return Key((TCb)callback) as Type;
        }
    }

    public class CovariantPolicyBuilder<TCb>
        : CallbackPolicyBuilder<CovariantPolicy<TCb>, CovariantPolicyBuilder<TCb>>
    {
        public CovariantPolicyBuilder(CovariantPolicy<TCb> policy)
            : base(policy)
        {
        }

        public CallbackArgument<TCb> Callback  => CallbackArgument<TCb>.Instance;
        public ReturnsKey           ReturnKey => ReturnsKey.Instance;

        public ExtractArgument<TCb, TRes> Extract<TRes>(Func<TCb, TRes> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<TCb, TRes>(extract);
        }

        public CovariantPolicyBuilder<TCb> MatchMethodWithCallback(params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<TCb>))
                MatchMethod(args.Concat(new [] { Callback }).ToArray());
            MatchMethod(args);
            return this;
        }

        public CovariantPolicyBuilder<TCb> MatchMethodWithCallback(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<TCb>))
                MatchMethod(returnRule, args.Concat(new[] { Callback }).ToArray());
            MatchMethod(returnRule, args);
            return this;
        }
    }
}
