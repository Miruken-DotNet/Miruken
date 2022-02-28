namespace Miruken.Context;

public delegate void ContextChangingDelegate(
    IContextual contextual,
    Context oldContext, ref Context newContext);

public delegate void ContextChangedDelegate(
    IContextual contextual,
    Context oldContext, Context newContext);

public interface IContextual
{
    Context Context { get; set; }

    event ContextChangingDelegate ContextChanging;
    event ContextChangedDelegate ContextChanged;
}