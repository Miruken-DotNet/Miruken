namespace Miruken.Callback.Policy
{
    public class ReturnsBoolOrVoid<Attrib> : ReturnRule<Attrib>
        where Attrib : DefinitionAttribute
    {
        public static readonly ReturnsBoolOrVoid<Attrib>
            Instance = new ReturnsBoolOrVoid<Attrib>();

        private ReturnsBoolOrVoid()
        {
        }

        public override bool Matches(MethodDefinition<Attrib> method)
        {
            return method.IsVoid || method.ReturnType == typeof(bool);
        }
    }
}
