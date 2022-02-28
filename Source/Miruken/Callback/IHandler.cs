namespace Miruken.Callback;

using System;

public interface IHandler : IServiceProvider, IProtocolAdapter
{
	bool Handle(object callback, ref bool greedy, IHandler composer = null);
}