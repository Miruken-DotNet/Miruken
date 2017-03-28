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

        public bool Matches(Attrib definition, MethodInfo method)
        {
            if (_returnValue != null &&
                !_returnValue.Matches(definition, method.ReturnType))
                return false;

            var parameters = method.GetParameters();
            return _args.Length == parameters.Length &&
                   _args.Zip(parameters, (arg, param) => arg.Matches(definition, param))
                        .All(m => m);
        }

        public object[] ResolveArgs(Attrib definition, object callback, IHandler handler)
        {
            return _args
                .Select(arg => arg.Resolve(definition, callback, handler))
                .ToArray();
        }
    }
}