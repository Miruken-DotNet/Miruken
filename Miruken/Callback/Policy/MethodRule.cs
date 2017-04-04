namespace Miruken.Callback.Policy
{
    using System.Linq;
    using System.Reflection;

    public class MethodRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly ArgumentRule<Attrib>[] _args;
        private readonly ReturnRule<Attrib> _returnValue;

        public MethodRule(params ArgumentRule<Attrib>[] args)
        {
            _args = args;
        }

        public MethodRule(ReturnRule<Attrib> returnValue, params ArgumentRule<Attrib>[] args)
        {
            _returnValue = returnValue;
            _args        = args;
        }

        public bool Matches(MethodInfo method, Attrib attribute)
        {
            if (_returnValue != null && 
                !_returnValue.Matches(method.ReturnType, attribute))
                return false;

            var parameters = method.GetParameters();
            return _args.Length == parameters.Length &&
                   _args.Zip(parameters, (arg, param) => arg.Matches(param, attribute))
                        .All(m => m);
        }

        public void Configure(MethodDefinition<Attrib> method)
        {
            _returnValue?.Configure(method);
            var parameters = method.Method.GetParameters();
            for (var i = 0; i < parameters.Length; ++i)
                _args[i].Configure(parameters[i], method);
        }

        public object[] ResolveArgs(object callback, IHandler handler)
        {
            return _args.Select(arg => arg.Resolve(callback, handler))
                        .ToArray();
        }
    }
}