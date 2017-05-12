namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public delegate PolicyMethodBinding MethodBinder(
        MethodRule rule, MethodDispatch dispatch, DefinitionAttribute attribute);

    public class MethodRule
    {
        private readonly ArgumentRule[] _args;
        private readonly ReturnRule _returnValue;
        private readonly MethodBinder _binder;
        private readonly int _minArgs;

        public MethodRule(MethodBinder binder, params ArgumentRule[] args)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));
            _minArgs = args.Length - args.Aggregate(0, (opt, arg) =>
            {
                if (arg is IOptional) return opt + 1;        
                if (opt > 0)
                    throw new ArgumentException(
                        "Optional arguments must appear after all required arguments");
                return opt;
            });
            _binder = binder;
            _args   = args;
        }

        public MethodRule(MethodBinder binder, ReturnRule returnValue,
                          params ArgumentRule[] args)
            : this(binder, args)
        {
            _returnValue = returnValue;
        }

        public bool Matches(MethodInfo method, DefinitionAttribute attribute)
        {
            var parameters = method.GetParameters();
            var paramCount = parameters.Length;
            var aliases    = new Dictionary<string, Type>();
            return paramCount >= _minArgs && paramCount <= _args.Length &&
                   parameters.Zip(_args, (param, arg) => 
                       arg.Matches(param, attribute, aliases)).All(m => m)
                && _returnValue?.Matches(method.ReturnType, parameters,
                                         attribute, aliases) != false;
        }

        public PolicyMethodBinding Bind(MethodDispatch dispatch, DefinitionAttribute attribute)
        {
            var binding = _binder(this, dispatch, attribute);
            _returnValue?.Configure(binding);
            var parameters = dispatch.Method.GetParameters();
            for (var i = 0; i < parameters.Length; ++i)
                _args[i].Configure(parameters[i], binding);
            return binding;
        }

        public object[] ResolveArgs(
            PolicyMethodBinding method, object callback, IHandler handler)
        {
            return _args.Take(method.Dispatcher.Parameters.Length)
                        .Select(arg => arg.Resolve(callback, handler))
                        .ToArray();
        }
    }
}