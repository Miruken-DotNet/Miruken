namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Infrastructure;

    public class TargetArgument<TCb> : ArgumentRule
    {
        private readonly Func<TCb, object> _target;
        private string _alias;

        public TargetArgument(Func<TCb, object> target)
        {
            _target = target 
                   ?? throw new ArgumentNullException(nameof(target));
        }

        public TargetArgument<TCb> this[string alias]
        {
            get
            {
                if (string.IsNullOrEmpty(alias))
                    throw new ArgumentException(@"Alias cannot be empty", nameof(alias));
                _alias = alias;
                return this;
            }
        }

        public override bool Matches(
            ParameterInfo parameter, CategoryAttribute category,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            if (paramType.Is<TCb>()) return false;
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
            var restrict = category.InKey as Type;
            if (restrict == null || paramType.Is(restrict) || restrict.Is(paramType))
            {
                if (_alias != null)
                    aliases.Add(_alias, paramType);
                return true;
            }
            throw new InvalidOperationException(
                $"Key {restrict.FullName} is not related to {paramType.FullName}");
        }

        public override void Configure(ParameterInfo parameter,
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            var key       = policyMethodBindingInfo.InKey;
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
                (restrict == null || paramType.Is(restrict)))
                policyMethodBindingInfo.InKey = paramType;
        }

        public override object Resolve(object callback)
        {
            return callback is TCb cb ? _target(cb) : callback;
        }
    }
}