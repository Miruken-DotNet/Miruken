namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public class TargetArgument<Attrib, Cb> : ArgumentRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly Func<Cb, object> _target;

        public TargetArgument(Func<Cb, object> target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            _target = target;
        }

        public override bool Matches(ParameterInfo parameter, Attrib attribute)
        {
            var restrict  = attribute.Key as Type;
            var paramType = parameter.ParameterType;
            if (restrict == null || restrict.IsAssignableFrom(paramType)
                || paramType.IsAssignableFrom(restrict))
                return true;
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {paramType.FullName}");
        }

        public override void Configure(
            ParameterInfo parameter, MethodDefinition<Attrib> method)
        {
            var paramType = parameter.ParameterType;
            if (paramType == typeof(object)) return;
            method.VarianceType = paramType;
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return _target((Cb)callback);
        }
    }
}