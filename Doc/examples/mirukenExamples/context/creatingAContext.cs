namespace Example.mirukenExamples.context
{
    using Miruken.Context;

    public class CreatingAContext
    {
        public Context Context { get; set; }

        public CreatingAContext()
        {
            Context = new Context();
        }
    }
}