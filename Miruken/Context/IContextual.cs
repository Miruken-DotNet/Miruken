namespace Miruken.Context
{
    public delegate void ContextDelegate(IContext oldContext, IContext neContext);

	public interface IContextual<TContext>
        where TContext : class, IContext<TContext>
	{
		TContext Context { get; set; }

	    event ContextDelegate ContextChanging;

        event ContextDelegate ContextChanged;
    }

    public interface IContextual : IContextual<IContext>
    {       
    }
}
