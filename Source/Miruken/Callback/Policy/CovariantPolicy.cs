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

        protected static int? Accuracy(Type type, Type key)
        {
            if (type == key) return 0;
            if (type == null || key == null)
                return null;
            if (type.IsGenericTypeDefinition)
                return key.GetOpenTypeConformance(type) != null
                    ? 1000 : (int?)null;
            if (key.IsGenericTypeDefinition)
                return type.IsGenericType && type.GetGenericTypeDefinition() == key
                    ? 2000 : (int?)null;
            return type.IsAssignableFrom(key)
                 ? GetAccuracy(type, key)
                 : (int?)null;
        }

        private static int GetAccuracy(Type type, Type key)
        {
            if (type.IsClass && key.IsClass)
            {
                var accuracy = 0;
                while (key != null && key != type)
                {
                    key = key.BaseType;
                    ++accuracy;
                }
                return accuracy;
            }
            return 50;
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

        public override IEnumerable<Tuple<object, int>> GetCompatibleKeys(
            object key, IEnumerable output)
        {
            var type = key as Type;
            if (type == null)
                return output.Cast<object>()
                    .Where(k => !Equals(key, k) && Equals(k, key))
                    .Select(k => Tuple.Create(k, 1));

            if (type == typeof(object))
                return output.Cast<object>()
                    .Where(t => (t as Type)?.IsGenericTypeDefinition != true)
                    .Select(t => Tuple.Create(t, 10000));

            return output.OfType<Type>()
                .Where(t => t != type)
                .Select(t => new { Key = t, Accuracy = Accuracy(type, t) })
                .Where(k => k.Accuracy.HasValue)
                .OrderBy(k => k.Accuracy)
                .Select(k => Tuple.Create((object)k.Key, k.Accuracy.Value));
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
