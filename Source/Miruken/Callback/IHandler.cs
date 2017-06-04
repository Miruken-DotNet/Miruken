namespace Miruken.Callback
{
    using System;

    public interface IHandler : IServiceProvider, IProtocolAdapter
	{
		bool Handle(object callback, bool greedy = false, IHandler composer = null);
	}
}
