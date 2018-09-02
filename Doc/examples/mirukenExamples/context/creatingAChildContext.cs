namespace Example.mirukenExamples.context
{
    using Miruken.Context;

    public class CreatingAChildContext
    {
        public Context Parent { get; set; }
        public Context Child  { get; set; }

        public CreatingAChildContext()
        {
            Parent = new Context();
            Child  = Parent.CreateChild();
        }
    }
}