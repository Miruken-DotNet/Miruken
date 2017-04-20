namespace Miruken.Callback.Policy
{
    public class ContravariantMethod : MethodBinding
    {
        public ContravariantMethod(MethodRule rule,
                                   MethodDispatch dispatch,
                                   DefinitionAttribute attribute)
            : base(rule, dispatch, attribute)
        {        
        }

        protected override object NoResult => false;

        public override bool Dispatch(
            object target, object callback, IHandler composer)
        {
            var result = Invoke(target, callback, composer);
            return result == null || true.Equals(result);
        }
    }
}
