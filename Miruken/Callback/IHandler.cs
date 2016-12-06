using System;

namespace Miruken.Callback
{
    public interface ICallback
    {
        Type   ResultType { get; }
        object Result     { get; set; }
    }

	public interface IHandler : IProtocolAdapter
	{
		bool Handle(object callback, bool greedy = false, IHandler composer = null);
	}
}
