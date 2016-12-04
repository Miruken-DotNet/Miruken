using System;

namespace Miruken.Callback
{
    public interface ICallback
    {
        Type   ResultType { get; }
        object Result     { get; set; }
    }

	public interface ICallbackHandler : IProtocolAdapter
	{
		bool Handle(object callback, bool greedy = false, ICallbackHandler composer = null);
	}
}
