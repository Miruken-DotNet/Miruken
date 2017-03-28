namespace Miruken.Callback.Policy
{
    public interface ICallbackFilter
    {
        bool Accepts(DefinitionAttribute definition, object callback, IHandler composer);
    }
}
