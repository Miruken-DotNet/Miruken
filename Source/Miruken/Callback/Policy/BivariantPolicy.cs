namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Bindings;
    using Rules;

    public abstract class BivariantPolicy : CallbackPolicy
    {
        public override object CreateKey(PolicyMemberBindingInfo bindingInfo)
        {
            var inKey  = bindingInfo.InKey;
            var outKey = bindingInfo.OutKey;
            return inKey != null && outKey != null
                 ? Tuple.Create(outKey, inKey)
                 : null;
        }

        public static BivariantPolicy<TCb> Create<TCb>(
            Func<TCb, object> output, Func<TCb, object> input,
            Action<BivariantPolicyBuilder<TCb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new BivariantPolicy<TCb>(output, input);
            var builder = new BivariantPolicyBuilder<TCb>(policy);
            build(builder);
            return policy;
        }
    }

    public class BivariantPolicy<TCb> : BivariantPolicy
    {
        public BivariantPolicy(
            Func<TCb, object> input, 
            Func<TCb, object> output)
        {
            Output        = new CovariantPolicy<TCb>(input);
            Input         = new ContravariantPolicy<TCb>(output);
            GetResultType = Output.GetResultType;
            AcceptResult  = Output.AcceptResult;
        }

        public CovariantPolicy<TCb>     Output { get; }
        public ContravariantPolicy<TCb> Input  { get; }

        public override object GetKey(object callback)
        {
            return (callback as ICallbackKey)?.Key
                ?? Tuple.Create(Output.GetKey(callback), Input.GetKey(callback));
        }

        public override IEnumerable<object> GetCompatibleKeys(
            object key, IEnumerable available)
        {
            if (!(key is Tuple<object, object> tuple))
                return Enumerable.Empty<object>();
            var input  = tuple.Item2;
            var output = tuple.Item1;
            return available.OfType<Tuple<object, object>>().Select(testKey =>
            {
                var inKey    = testKey.Item2;
                var outKey   = testKey.Item1;
                var inEqual  = Equals(input, inKey);
                var outEqual = Equals(output, outKey);
                if (inEqual && outEqual) return null;
                if (!outEqual)
                {
                    using var k = Output.GetCompatibleKeys(output,
                        new[] { outKey }).GetEnumerator();
                    if (!k.MoveNext() || k.Current == null)
                        return null;
                }
                if (!inEqual)
                {
                    using var k = Input.GetCompatibleKeys(input,
                        new[] { inKey }).GetEnumerator();
                    if (!k.MoveNext() || k.Current == null)
                        return null;
                }
                return testKey;
            }).Where(k => k != null);
        }

        public override int Compare(object key1, object key2)
        {
            var tuple1 = key1 as Tuple<object, object>;
            var tuple2 = key2 as Tuple<object, object>;
            var order  = Input.Compare(tuple1?.Item2, tuple2?.Item2);
            return order == 0
                 ? Output.Compare(tuple1?.Item1, tuple2?.Item1)
                 : order;
        }
    }

    public class BivariantPolicyBuilder<TCb>
       : CallbackPolicyBuilder<BivariantPolicy<TCb>, BivariantPolicyBuilder<TCb>>
    {
        public BivariantPolicyBuilder(BivariantPolicy<TCb> policy)
            : base(policy)
        {
        }

        public CallbackArgument<TCb> Callback  => CallbackArgument<TCb>.Instance;
        public TargetArgument<TCb>   Target    => new TargetArgument<TCb>(Policy.Input.Target);
        public ReturnsKey            ReturnKey => ReturnsKey.Instance;

        public ExtractArgument<TCb, TRes> Extract<TRes>(Func<TCb, TRes> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            return new ExtractArgument<TCb, TRes>(extract);
        }

        public BivariantPolicyBuilder<TCb> MatchCallbackMethod(
            ReturnRule returnRule, params ArgumentRule[] args)
        {
            if (!args.Any(arg => arg is CallbackArgument<TCb>))
                MatchMethod(returnRule, args.Concat(new[] { Callback }).ToArray());
            MatchMethod(returnRule, args);
            return this;
        }
    }
}
