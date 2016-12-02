using System;
using Miruken.Callback;
using Miruken.Graph;

namespace Miruken.Context
{
    public enum ContextState
    {
        Active = 0,
        Ending,
        Ended
    }

	public interface IContext<TContext>
        : ICompositeCallbackHandler, ICallbackHandlerAxis, ITraversing, IDisposable
		where TContext : class, IContext<TContext>
	{
        event Action<TContext> ContextEnding;
        event Action<TContext> ContextEnded;
        event Action<TContext> ChildContextEnding;
        event Action<TContext> ChildContextEnded;

        ContextState State { get; }

		new TContext Parent { get; }

        TContext Root { get; }

        bool HasChildren { get; }

		TContext CreateChild();

        new TContext[] Children { get; }

	    void Store(object data);

	    TContext UnwindToRoot();

        TContext Unwind();

        void End();
	}

    public interface IContext : IContext<IContext>
    {
    }
}
