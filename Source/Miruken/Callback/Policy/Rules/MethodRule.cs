namespace Miruken.Callback.Policy.Rules
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Bindings;

    public delegate PolicyMemberBinding MemberBinder(
        PolicyMemberBindingInfo policyMemberBindingInfo);

    public class MethodRule
    {
        private readonly MemberBinder _binder;

        public MethodRule(MemberBinder binder, params ArgumentRule[] args)
        {
            _binder = binder 
                ?? throw new ArgumentNullException(nameof(binder));
            Args = args;
        }

        public MethodRule(MemberBinder binder, ReturnRule returnValue,
                          params ArgumentRule[] args)
            : this(binder, args)
        {
            ReturnValue = returnValue;
        }

        public ArgumentRule[] Args        { get; }
        public ReturnRule     ReturnValue { get; }

        public bool Matches(MethodInfo method, CategoryAttribute category)
        {
            var parameters = method.GetParameters();
            var context    = new RuleContext(category);
            return ReturnValue?.Matches(
                method.ReturnType, parameters, context) != false &&
                parameters.Length >= Args.Length &&
                parameters.Zip(Args, (param, arg) => arg.Matches(param, context))
                    .All(m => m);
        }

        public PolicyMemberBinding Bind(MemberDispatch dispatch, CategoryAttribute category)
        {
            var policyBindingInfo = new PolicyMemberBindingInfo(this, dispatch, category);
            ReturnValue?.Configure(policyBindingInfo);
            var parameters = dispatch.Member.GetParameters();
            for (var i = 0; i < Args.Length; ++i)
                Args[i].Configure(parameters[i], policyBindingInfo);
            return _binder(policyBindingInfo);
        }

        public object[] ResolveArgs(object callback)
        {
            return Args.Select(arg => arg.Resolve(callback)).ToArray();
        }
    }
}