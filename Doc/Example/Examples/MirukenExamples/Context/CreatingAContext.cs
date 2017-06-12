namespace Examples.MirukenExamples.Context
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