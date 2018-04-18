namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public abstract class ReturnRule
    {
        public ReturnVoid OrVoid => new ReturnVoid(this);
        public ReturnAsync Async => new ReturnAsync(this);

        public abstract bool Matches(
            Type returnType, ParameterInfo[] parameters,
            CategoryAttribute category,
            RuleContext context);

        public virtual void Configure(PolicyMethodBindingInfo policyMethodBindingInfo)
        {
        }

        public virtual R GetInnerRule<R>() where R : ReturnRule
        {
            return this as R;
        }

        public ReturnAlias Alias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentException(
                    @"Return alias cannot be empty", nameof(alias));
            return new ReturnAlias(alias, this);
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
            CategoryAttribute category, RuleContext context)
        {
            return Rule.Matches(returnType, parameters, category, context);
        }

        public override void Configure(
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            Rule.Configure(policyMethodBindingInfo);
        }

        public override R GetInnerRule<R>()
        {
            return base.GetInnerRule<R>() ?? Rule.GetInnerRule<R>();
        }
    }

    public class ReturnVoid : ReturnRuleDecorator
    {
        public ReturnVoid(ReturnRule rule) : base(rule)
        {
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            CategoryAttribute category, RuleContext context)
        {
            return returnType == typeof(void) ||
                   base.Matches(returnType, parameters, category, context);
        }

        public override void Configure(
            PolicyMethodBindingInfo policyMethodBindingInfo)
        {
            if (!policyMethodBindingInfo.Dispatch.IsVoid)
                Rule.Configure(policyMethodBindingInfo);
        }
    }

    public class ReturnAlias : ReturnRuleDecorator
    {
        private readonly string _alias;

        public ReturnAlias(string alias, ReturnRule rule)
            : base(rule)
        {
            _alias = alias;
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            CategoryAttribute category,
            RuleContext context)
        {
            return context.AddAlias(_alias, returnType) &&
                base.Matches(returnType, parameters, category, context);
        }
    }
}