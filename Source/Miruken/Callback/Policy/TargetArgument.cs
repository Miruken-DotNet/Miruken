namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Infrastructure;

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
            var paramType = parameter.ParameterType;
            if (paramType.Is<Cb>())
                return false;
            if (paramType.IsGenericParameter)
            {
                var contraints = paramType.GetGenericParameterConstraints();
                switch (contraints.Length)
                {
                    case 0:
                        paramType = typeof(object);
                        break;
                    case 1:
                        paramType = contraints[0];
                        break;
                    default:
                        return false;
                }
            }
            var restrict = attribute.Key as Type;
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

        public override void Configure(ParameterInfo parameter,
            ref PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            var key       = policyMethodBindingInfo.Key;
            var restrict  = key as Type;
            var paramType = parameter.ParameterType;
            policyMethodBindingInfo.CallbackIndex = parameter.Position;
            if (paramType.IsGenericParameter)
            {
                var contraints = paramType.GetGenericParameterConstraints();
                paramType = contraints.Length == 1
                          ? contraints[0]
                          : typeof(object);
            }
            if (paramType != typeof(object) &&
                (restrict == null || restrict.IsAssignableFrom(paramType)))
                policyMethodBindingInfo.Key = paramType;
        }

        public override object Resolve(object callback)
        {
            return callback is Cb ? _target((Cb)callback) : callback;
        }
    }
}