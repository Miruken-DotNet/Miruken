namespace Example.MirukenExamples.Context
{
    using Miruken.Context;

    public class AContextWithHandlerInstances
    {
        public Context Context { get; set; }

        public AContextWithHandlerInstances()
        {
            Context = new Context();
            Context.AddHandlers(new SomeHandler(), new AnotherHandler());
        }
    }
}