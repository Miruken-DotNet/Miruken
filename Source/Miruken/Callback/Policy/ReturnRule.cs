namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public abstract class ReturnRule
    {
        public ReturnVoid  OrVoid => new ReturnVoid(this);
        public ReturnAsync Async  => new ReturnAsync(this);

        public abstract bool Matches(
            Type returnType, ParameterInfo[] parameters,
            DefinitionAttribute attribute,
            IDictionary<string, Type> aliases);

        public virtual void Configure(PolicyMethodBindingInfo policyMethodBindingInfo) { }

        public virtual R GetSubRule<R>() where R : ReturnRule
        {
            return this as R;
        }

        protected static bool IsLogicalVoid(Type returnType)
        {
            return returnType == typeof(void);
        }
    }

    public abstract class ReturnRuleDecorator : ReturnRule
    {
        protected ReturnRuleDecorator(ReturnRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            Rule = rule;
        }

        public ReturnRule Rule { get; }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            return Rule.Matches(returnType, parameters, attribute, aliases);
        }

        public override void Configure(
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            Rule.Configure(policyMethodBindingInfo);
        }

        public override R GetSubRule<R>()
        {
            return base.GetSubRule<R>() ?? Rule.GetSubRule<R>();
        }
    }

    public class ReturnVoid : ReturnRuleDecorator
    {
        public ReturnVoid(ReturnRule rule) : base(rule)
        {
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            return returnType == typeof(void) ||
                base.Matches(returnType, parameters, attribute, aliases);
        }

        public override void Configure(
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            if (!policyMethodBindingInfo.Dispatch.IsVoid)
                Rule.Configure(policyMethodBindingInfo);
        }
    }
}