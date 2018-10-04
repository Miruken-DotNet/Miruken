namespace Miruken.Callback.Policy.Rules
{
    using System;
    using System.Reflection;
    using Bindings;
    using Infrastructure;

    public class CallbackArgument : ArgumentRule
    {
        public static readonly CallbackArgument
            Instance = new CallbackArgument();

        public override bool Matches(
            ParameterInfo parameter, RuleContext context)
        {
            var paramType = parameter.ParameterType;
            if (paramType.IsGenericParameter)
            {
                var constraints = paramType.GetGenericParameterConstraints();
                switch (constraints.Length)
                {
                    case 0:
                        paramType = typeof(object);
                        break;
                    case 1:
                        paramType = constraints[0];
                        break;
                    default:
                        return false;
                }
            }
            var restrict = context.Category.InKey as Type;
            if (restrict == null || paramType.Is(restrict) || restrict.Is(paramType))
                return true;

            context.AddError(
                $"Key {restrict.FullName} is not related to {paramType.FullName}");
            return false;
        }

        public override void Configure(ParameterInfo parameter,
            PolicyMemberBindingInfo policyMemberBindingInfo)
        {
            var key       = policyMemberBindingInfo.InKey;
            var restrict  = key as Type;
            policyMemberBindingInfo.CallbackIndex = parameter.Position;
            if (key != null && restrict == null) return;
            var paramType = parameter.ParameterType;
            if (paramType.IsGenericParameter)
            {
                var constraints = paramType.GetGenericParameterConstraints();
                paramType = constraints.Length == 1
                          ? constraints[0]
                          : typeof(object);
            }
            if (paramType != typeof(object) &&
                (restrict == null || paramType.Is(restrict)))
                policyMemberBindingInfo.InKey = paramType;
        }

        public override object Resolve(object callback)
        {
            return callback;
        }
    }

    public class CallbackArgument<TCb> : ArgumentRule
    {
        public static readonly CallbackArgument<TCb>
             Instance = new CallbackArgument<TCb>();

        private CallbackArgument()
        {         
        }

        public override bool Matches(
            ParameterInfo parameter, RuleContext context)
        {
            var paramType = parameter.ParameterType;
            return paramType.Is<TCb>();
        }

        public override void Configure(ParameterInfo parameter,
            PolicyMemberBindingInfo policyMemberBindingInfo) { }

        public override object Resolve(object callback)
        {
            return callback;
        }
    }
}