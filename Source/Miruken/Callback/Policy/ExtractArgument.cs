namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;
    using Infrastructure;

    public class ExtractArgument<Cb, Res> : ArgumentRule
    {
        private readonly Func<Cb, Res> _extract;

        public ExtractArgument(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            _extract = extract;
        }

        public override bool Matches(
            ParameterInfo parameter, RuleContext context)
        {
            var paramType = parameter.ParameterType;
            return paramType.Is<Res>();
        }

        public override object Resolve(object callback)
        {
            return _extract((Cb)callback);
        }
    }
}