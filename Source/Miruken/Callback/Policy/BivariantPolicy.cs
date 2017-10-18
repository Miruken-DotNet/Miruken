namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
            Func<Cb, object> output, Func<Cb, object> input,
            Action<BivariantPolicyBuilder<Cb>> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var policy  = new BivariantPolicy<Cb>(output, input);
            var builder = new BivariantPolicyBuilder<Cb>(policy);
            build(builder);
            return policy;
        }
    }

    public class BivariantPolicy<Cb> : BivariantPolicy
    {
        public BivariantPolicy(
            Func<Cb, object> input, 
            Func<Cb, object> ouput)
        {
            Output       = new CovariantPolicy<Cb>(input);
            Input        = new ContravariantPolicy<Cb>(ouput);
            ResultType   = Output.ResultType;
            AcceptResult = Output.AcceptResult;
        }

        public CovariantPolicy<Cb>     Output { get; }
        public ContravariantPolicy<Cb> Input  { get; }

        public override object GetKey(object callback)
        {
            return Tuple.Create(
                Output.GetKey(callback),
                Input.GetKey(callback));
        }

        public override IEnumerable<Tuple<object, int>> GetCompatibleKeys(
            object key, IEnumerable keys)
        {
            var tuple = key as Tuple<object, object>;
            if (tuple == null) return Enumerable.Empty<Tuple<object, int>>();
            var input  = tuple.Item2;
            var output = tuple.Item1;
            return keys.OfType<Tuple<object, object>>().Select(testKey =>
            {
                var inKey    = testKey.Item2;
                var outKey   = testKey.Item1;
                var inEqual  = Equals(input, inKey);
                var outEqual = Equals(output, outKey);
                if (inEqual && outEqual) return null;
                var accuracy = 0;
                if (!outEqual)
                {
                    using (var k = Output.GetCompatibleKeys(output,
                        new[] { outKey }).GetEnumerator())
                    {
                        if (!k.MoveNext() || k.Current == null)
                            return null;
                        accuracy += k.Current.Item2;
                    }
                }
                if (!inEqual)
                {
                    using (var k = Input.GetCompatibleKeys(input,
                        new[] { inKey }).GetEnumerator())
                    {
                        if (!k.MoveNext() || k.Current == null)
                            return null;
                        accuracy += k.Current.Item2;
                    }
                }
                return Tuple.Create((object)testKey, accuracy);
            }).Where(k => k != null);
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
