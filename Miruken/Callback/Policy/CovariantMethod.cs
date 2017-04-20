namespace Miruken.Callback.Policy
{
    using System;

    public class CovariantMethod : MethodBinding
    {
        private readonly Func<object, Type> _returnType;

        public CovariantMethod(MethodRule rule,
                               MethodDispatch dispatch,
                               DefinitionAttribute attribute,
                               Func<object, Type> returnType)
            : base(rule, dispatch, attribute)
        {
            _returnType = returnType;
        }

        public override bool Dispatch(
            object target, object callback, IHandler composer)
        {
            var returnType = _returnType(callback);
            var result = Invoke(target, callback, composer, returnType);
            return result != null;
        }
    }
}
