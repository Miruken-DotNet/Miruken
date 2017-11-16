namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public abstract class CovariantPolicy : CallbackPolicy
    {
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

        public override int Compare(object key1, object key2)
        {
            if (key1 == key2) return 0;
            var type1 = key1 as Type;
            if (type1 == null) return 1;
            var type2 = key2 as Type;
            if (type2 == null) return -1;
            if (type2.ContainsGenericParameters)
                return -1;
            if (type1.ContainsGenericParameters)
                return 1;
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
