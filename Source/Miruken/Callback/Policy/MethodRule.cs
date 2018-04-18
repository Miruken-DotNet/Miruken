namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;
    using System.Reflection;

    public delegate PolicyMethodBinding MethodBinder(
        PolicyMethodBindingInfo policyMethodBindingInfo);

    public class MethodRule
    {
        private readonly MethodBinder _binder;

        public MethodRule(MethodBinder binder, params ArgumentRule[] args)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));
            _binder = binder;
            Args    = args;
        }

        public MethodRule(MethodBinder binder, ReturnRule returnValue,
                          params ArgumentRule[] args)
            : this(binder, args)
        {
            ReturnValue = returnValue;
        }

        public ArgumentRule[] Args        { get; }
        public ReturnRule     ReturnValue { get; }

        public bool Matches(MethodInfo method, CategoryAttribute category)
        {
            var parameters  = method.GetParameters();
            var paramCount  = parameters.Length;
            var context     = new RuleContext();
            if (ReturnValue?.Matches( method.ReturnType,
                parameters, category, context) == false) return false;
            return paramCount >= Args.Length && parameters.Zip(
                Args, (param, arg) => arg.Matches(param, category, context))
                .All(m => m);
        }

        public PolicyMethodBinding Bind(
            MethodDispatch dispatch, CategoryAttribute category)
        {
            var policyBindingInfo = new PolicyMethodBindingInfo(this, dispatch, category);
            ReturnValue?.Configure(policyBindingInfo);
            var parameters = dispatch.Method.GetParameters();
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