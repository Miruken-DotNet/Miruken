namespace Miruken.Callback.Policy.Rules
{
    using System;
    using System.Reflection;
    using Bindings;

    public abstract class ReturnRule
    {
        public ReturnVoid  OrVoid => new(this);
        public ReturnAsync Async  => new(this);

        public abstract bool Matches(
            Type returnType, ParameterInfo[] parameters,
            RuleContext context);

        public virtual void Configure(PolicyMemberBindingInfo policyMemberBindingInfo)
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
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public ReturnRule Rule { get; }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            RuleContext context)
        {
            return Rule.Matches(returnType, parameters, context);
        }

        public override void Configure(
            PolicyMemberBindingInfo policyMemberBindingInfo)
        {
            Rule.Configure(policyMemberBindingInfo);
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
            RuleContext context)
        {
            return returnType == typeof(void) ||
                   base.Matches(returnType, parameters, context);
        }

        public override void Configure(
            PolicyMemberBindingInfo policyMemberBindingInfo)
        {
            if (!policyMemberBindingInfo.Dispatch.IsVoid)
                Rule.Configure(policyMemberBindingInfo);
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
            RuleContext context)
        {
            return base.Matches(returnType, parameters, context) &&
                context.AddAlias(_alias, returnType);
        }
    }
}