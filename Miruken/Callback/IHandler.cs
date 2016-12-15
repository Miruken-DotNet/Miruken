namespace Miruken.Callback
{
    public interface IHandler : IProtocolAdapter
	{
		bool Handle(object callback, bool greedy = false, IHandler composer = null);
	}
}
