namespace Miruken.Callback.Policy
{
    using System.Linq;

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

        public bool Matches(MethodDefinition<Attrib> method)
        {
            if (_returnValue != null && !_returnValue.Matches(method))
                return false;

            var parameters = method.Method.GetParameters();
            return _args.Length == parameters.Length &&
                   _args.Zip(parameters, (arg, param) => arg.Matches(method, param))
                        .All(m => m);
        }

        public object[] ResolveArgs(object callback, IHandler handler)
        {
            return _args.Select(arg => arg.Resolve(callback, handler))
                        .ToArray();
        }
    }
}