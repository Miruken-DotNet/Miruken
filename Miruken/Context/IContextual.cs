namespace Miruken.Context
{
    public delegate void ContextDelegate<TContext>(
        IContextual<TContext> contextual,
        TContext oldContext, TContext neContext)
            where TContext : class, IContext<TContext>;

    public interface IContextual<TContext>
        where TContext : class, IContext<TContext>
	{
		TContext Context { get; set; }

	    event ContextDelegate<TContext> ContextChanging;

        event ContextDelegate<TContext> ContextChanged;
    }

    public interface IContextual : IContextual<IContext>
    {
    }
}
