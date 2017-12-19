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
            CategoryAttribute category,
            IDictionary<string, Type> aliases);

        public virtual void Configure(PolicyMethodBindingInfo policyMethodBindingInfo) { }

        public virtual T GetSubRule<T>() where T : ReturnRule
        {
            return this as T;
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
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public ReturnRule Rule { get; }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            CategoryAttribute category,
            IDictionary<string, Type> aliases)
        {
            return Rule.Matches(returnType, parameters, category, aliases);
        }

        public override void Configure(
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            Rule.Configure(policyMethodBindingInfo);
        }

        public override T GetSubRule<T>()
        {
            return base.GetSubRule<T>() ?? Rule.GetSubRule<T>();
        }
    }

    public class ReturnVoid : ReturnRuleDecorator
    {
        public ReturnVoid(ReturnRule rule) : base(rule)
        {
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            CategoryAttribute category,
            IDictionary<string, Type> aliases)
        {
            return returnType == typeof(void) ||
                base.Matches(returnType, parameters, category, aliases);
        }

        public override void Configure(
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            if (!policyMethodBindingInfo.Dispatch.IsVoid)
                Rule.Configure(policyMethodBindingInfo);
        }
    }
}