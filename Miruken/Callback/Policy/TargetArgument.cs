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

        public override bool Matches(Attrib definition, ParameterInfo parameter)
        {
            Type varianceType;
            var restrict  = definition.Key as Type;
            var paramType = parameter.ParameterType;
            if (restrict == null || restrict.IsAssignableFrom(paramType))
                varianceType = paramType;
            else if (paramType.IsAssignableFrom(restrict))
                varianceType = restrict;
            else
                throw new InvalidOperationException(
                    $"Key {restrict.FullName} is not related to {paramType.FullName}");
            definition.VarianceType = varianceType;
            definition.AddFilters(CreateTypeFilter(definition, typeof(Cb)),
                CreateTypeFilter(definition, varianceType, cb => _target((Cb)cb)));
            return true;
        }

        public override object Resolve(Attrib definition, object callback, IHandler composer)
        {
            return _target((Cb)callback);
        }
    }
}