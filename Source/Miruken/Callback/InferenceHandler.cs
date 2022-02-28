namespace Miruken.Callback;

using Policy;

public class InferenceHandler : Handler
{
    private readonly IHandlerDescriptorFactory _factory;

    public InferenceHandler(IHandlerDescriptorFactory factory)
    {
        _factory = factory;
    }
}