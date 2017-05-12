namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class TargetArgument<Cb> : ArgumentRule
    {
        private readonly Func<Cb, object> _target;
        private string _alias;

        public TargetArgument(Func<Cb, object> target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            _target = target;
        }

        public TargetArgument<Cb> this[string alias]
        {
            get
            {
                if (string.IsNullOrEmpty(alias))
                    throw new ArgumentException("Alias cannot be empty", nameof(alias));
                _alias = alias;
                return this;
            }
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            var restrict  = attribute.Key as Type;
            var paramType = parameter.ParameterType;
            if (paramType.IsGenericParameter)
            {
                var contraints = paramType.GetGenericParameterConstraints();
                if (contraints.Length != 1) return false;
                paramType = contraints[0];
            }
            if (restrict == null || restrict.IsAssignableFrom(paramType)
                || paramType.IsAssignableFrom(restrict))
            {
                if (_alias != null)
                    aliases.Add(_alias, paramType);
                return true;
            }
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {paramType.FullName}");
        }

        public override void Configure(
            ParameterInfo parameter, PolicyMethodBinding binding)
        {
            base.Configure(parameter, binding);
            var paramType = parameter.ParameterType;
            if (paramType.IsGenericParameter)
            {
                var contraints = paramType.GetGenericParameterConstraints();
                paramType = contraints[0];
            }
            if (paramType == typeof(object)) return;
            binding.CallbackIndex = parameter.Position;
            binding.VarianceType  = paramType;
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return _target((Cb)callback);
        }
    }
}