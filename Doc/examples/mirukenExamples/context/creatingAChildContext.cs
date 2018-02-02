namespace Example.MirukenExamples.Context
{
    using Miruken.Context;

    public class CreatingAChildContext
    {
        public IContext Parent { get; set; }
        public IContext Child  { get; set; }

        public CreatingAChildContext()
        {
            Parent = new Context();
            Child  = Parent.CreateChild();
        }
    }
}