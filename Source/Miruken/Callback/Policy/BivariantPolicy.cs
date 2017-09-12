﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Linq;

    public abstract class BivariantPolicy : CallbackPolicy
    {
        public override object CreateKey(PolicyMethodBindingInfo bindingInfo)
        {
            var inKey  = bindingInfo.InKey;
            var outKey = bindingInfo.OutKey;
            return inKey != null && outKey != null
                 ? Tuple.Create(outKey, inKey)
                 : null;
        }

        public static BivariantPolicy<Cb> Create<Cb>(
            Func<Cb, object> key, Func<Cb, object> target,
            Action<BivariantPolicyBuilder<Cb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new BivariantPolicy<Cb>(key, target);
            var builder = new BivariantPolicyBuilder<Cb>(policy);
            build(builder);
            return policy;
        }
    }

    public class BivariantPolicy<Cb> : BivariantPolicy
    {
        public BivariantPolicy(
            Func<Cb, object> key, Func<Cb, object> target)
        {
            Output = new CovariantPolicy<Cb>(key);
            Input  = new ContravariantPolicy<Cb>(target);
        }

        public CovariantPolicy<Cb>     Output { get; }
        public ContravariantPolicy<Cb> Input  { get; }

        public override object GetKey(object callback)
        {
            return Tuple.Create(
                Output.GetKey(callback),
                Input.GetKey(callback));
        }

        public override IEnumerable GetCompatibleKeys(object key, IEnumerable keys)
        {
            var tuple = key as Tuple<object, object>;
            if (tuple == null) return Enumerable.Empty<object>();
            var inKey  = tuple.Item2;
            var outKey = tuple.Item1;
            return keys.OfType<Tuple<object, object>>().Where(testKey =>
                (Equals(testKey.Item1, outKey) || Output.GetCompatibleKeys(outKey,
                    new[] { testKey.Item1 }).GetEnumerator().MoveNext()) &&
                (Equals(testKey.Item2, inKey) ||Input.GetCompatibleKeys(inKey,
                    new[] { testKey.Item2 }).GetEnumerator().MoveNext()));
        }
    }

    public class BivariantPolicyBuilder<Cb>
       : CallbackPolicyBuilder<BivariantPolicy<Cb>, BivariantPolicyBuilder<Cb>>
    {
        public BivariantPolicyBuilder(BivariantPolicy<Cb> policy)
            : base(policy)
        {
        }

        public CallbackArgument<Cb> Callback  => CallbackArgument<Cb>.Instance;
        public TargetArgument<Cb>   Target    => new TargetArgument<Cb>(Policy.Input.Target);
        public ReturnsKey           ReturnKey => ReturnsKey.Instance;

        public ExtractArgument<Cb, Res> Extract<Res>(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<Cb, Res>(extract);
        }

        public BivariantPolicyBuilder<Cb> MatchCallbackMethod(params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<Cb>))
                MatchMethod(args.Concat(new[] { Callback }).ToArray());
            MatchMethod(args);
            return this;
        }

        public BivariantPolicyBuilder<Cb> MatchCallbackMethod(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<Cb>))
                MatchMethod(returnRule, args.Concat(new[] { Callback }).ToArray());
            MatchMethod(returnRule, args);
            return this;
        }
    }
}
