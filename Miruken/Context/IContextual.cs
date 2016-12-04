namespace Miruken.Context
{
	public interface IContextual<TContext>
        where TContext : class, IContext<TContext>
	{
		TContext Context { get; set; }
	}

    public interface IContextual : IContextual<IContext>
    {       
    }
}
