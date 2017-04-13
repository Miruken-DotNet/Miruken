namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;
    using System.Reflection;

    public class MethodRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly int _minArgs;
        private readonly ArgumentRule<Attrib>[] _args;
        private readonly ReturnRule<Attrib> _returnValue;

        public MethodRule(params ArgumentRule<Attrib>[] args)
        {
            _minArgs = args.Length - args.Aggregate(0, (opt, arg) =>
            {
                if (arg is IOptional) return opt + 1;        
                if (opt > 0)
                    throw new ArgumentException(
                        "Optional arguments must appear after all required arguments");
                return opt;
            });

            _args  = args;
        }

        public MethodRule(ReturnRule<Attrib> returnValue, params ArgumentRule<Attrib>[] args)
            : this(args)
        {
            _returnValue = returnValue;
        }

        public bool Matches(MethodInfo method, Attrib attribute)
        {
            if (_returnValue != null && 
                !_returnValue.Matches(method.ReturnType, attribute))
                return false;

            var parameters = method.GetParameters();
            var paramCount = parameters.Length;
            return paramCount >= _minArgs && paramCount <= _args.Length &&
                   parameters.Zip(_args, (param, arg) => arg.Matches(param, attribute))
                        .All(m => m);
        }

        public void Configure(MethodDefinition<Attrib> method)
        {
            _returnValue?.Configure(method);
            var parameters = method.Method.GetParameters();
            for (var i = 0; i < parameters.Length; ++i)
                _args[i].Configure(parameters[i], method);
        }

        public object[] ResolveArgs(
            MethodDefinition method, object callback, IHandler handler)
        {
            return _args.Take(method.ArgumentCount)
                        .Select(arg => arg.Resolve(callback, handler))
                        .ToArray();
        }
    }
}