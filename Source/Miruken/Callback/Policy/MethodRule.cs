namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    public delegate PolicyMethodBinding MethodBinder(
        PolicyMethodBindingInfo policyMethodBindingInfo);

    public class MethodRule
    {
        private readonly MethodBinder _binder;

        public MethodRule(MethodBinder binder, params ArgumentRule[] args)
        {
            _binder = binder 
                   ?? throw new ArgumentNullException(nameof(binder));
            Args = args;

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
            var parameters = method.GetParameters();
            var paramCount = parameters.Length;
            var aliases    = new Dictionary<string, Type>();
            if (paramCount < Args.Length || !parameters.Zip(Args, 
                (param, arg) => arg.Matches(param, category, aliases))
                .All(m => m)) return false;
            if (ReturnValue?.Matches(
                method.ReturnType, parameters, category, aliases) == false)
                throw new InvalidOperationException(
                     $"Method '{method.GetDescription()}' satisfied the arguments but rejected the return");
            return true;
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