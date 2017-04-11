namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;

    public class CovariantMethod<Attrib> : MethodDefinition<Attrib>
        where Attrib : DefinitionAttribute
    {
        private readonly Func<object, Type> _returnType;

        public CovariantMethod(MethodInfo method,
                               MethodRule<Attrib> rule,
                               Attrib attribute,
                               Func<object, Type> returnType)
            : base(method, rule, attribute)
        {
            _returnType = returnType;
        }

        protected override bool VerifyResult(object target, object callback, IHandler composer)
        {
            var result = Invoke(target, callback, composer);
            return result != null;
        }

        protected override object Invoke(object target, object callback,
            IHandler composer, Type returnType = null)
        {
            returnType = returnType ?? _returnType(callback);
            return base.Invoke(target, callback, composer, returnType);
        }
    }
}
