namespace Miruken.Callback.Policy.Rules
{
    using System;
    using System.Reflection;
    using Infrastructure;

    public class ExtractArgument<TCb, TRes> : ArgumentRule
    {
        private readonly Func<TCb, TRes> _extract;

        public ExtractArgument(Func<TCb, TRes> extract)
        {
            _extract = extract 
                ?? throw new ArgumentNullException(nameof(extract));
        }

        public override bool Matches(
            ParameterInfo parameter, RuleContext context)
        {
            var paramType = parameter.ParameterType;
            return paramType.Is<TRes>();
        }

        public override object Resolve(object callback)
        {
            return _extract((TCb)callback);
        }
    }
}