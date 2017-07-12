namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    public delegate PolicyMethodBinding MethodBinder(
        MethodRule rule, MethodDispatch dispatch, DefinitionAttribute attribute);

    public class MethodRule
    {
        private readonly ArgumentRule[] _args;
        private readonly ReturnRule _returnValue;
        private readonly MethodBinder _binder;

        public MethodRule(MethodBinder binder, params ArgumentRule[] args)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));
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
            if (paramCount < _args.Length ||
                !parameters.Zip(_args, (param, arg) => arg.Matches(param, attribute, aliases))
                .All(m => m)) return false;
            if (_returnValue?.Matches(method.ReturnType, parameters, attribute, aliases) == false)
                throw new InvalidOperationException(
                     $"Method '{method.GetDescription()}' satisfied the arguments but rejected the return");
            return true;
        }

        public PolicyMethodBinding Bind(MethodDispatch dispatch, DefinitionAttribute attribute)
        {
            var binding = _binder(this, dispatch, attribute);
            _returnValue?.Configure(binding);
            var parameters = dispatch.Method.GetParameters();
            for (var i = 0; i < _args.Length; ++i)
                _args[i].Configure(parameters[i], binding);
            return binding;
        }

        public object[] ResolveArgs(
            PolicyMethodBinding binding, object callback, IHandler handler)
        {
            return binding.Dispatcher.Parameters
                .Select((arg, i) => i < _args.Length
                    ? _args[i].Resolve(callback, binding, handler)
                    : ResolveDependency(binding, arg, handler)).ToArray();
        }

        private static object ResolveDependency(
            PolicyMethodBinding binding, ParameterInfo parameter, IHandler handler)
        {
            var paramType = parameter.ParameterType;
            if (paramType == typeof(IHandler))
                return handler;
            if (paramType.IsInstanceOfType(binding))
                return binding;
            return null;
        }
    }
}