namespace Miruken.Context
{
    public delegate void ContextChangingDelegate<TContext>(
        IContextual<TContext> contextual,
        TContext oldContext, ref TContext newContext)
            where TContext : class, IContext<TContext>;

    public delegate void ContextChangedDelegate<TContext>(
        IContextual<TContext> contextual,
        TContext oldContext, TContext newContext)
            where TContext : class, IContext<TContext>;

    public interface IContextual<TContext>
        where TContext : class, IContext<TContext>
	{
		TContext Context { get; set; }

	    event ContextChangingDelegate<TContext> ContextChanging;
        event ContextChangedDelegate<TContext> ContextChanged;
    }

    public interface IContextual : IContextual<IContext>
    {
    }
}
