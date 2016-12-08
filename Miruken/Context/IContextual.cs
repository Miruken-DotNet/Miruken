namespace Miruken.Context
{
    public delegate void ContextDelegate<in TContext>(TContext oldContext, TContext neContext);
    public delegate void ContextDelegate(IContext oldContext, IContext neContext);

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
