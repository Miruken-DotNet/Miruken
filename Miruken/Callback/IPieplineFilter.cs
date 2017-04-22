namespace Miruken.Callback
{
    using System.Collections.Generic;

    public delegate Res PipelineDelegate<out Res>(bool proceed = true);

    public interface IPipelineFilter
    {
        int? Order { get; set; }
    }

    public interface IPieplineFilter<in Cb, Res> : IPipelineFilter
    {
        Res Filter(Cb callback, IHandler composer, PipelineDelegate<Res> proceed);
    }

    public interface IPipleineFilterProvider
    {
        IEnumerable<IPipelineFilter> GetPipelineFilters(IHandler composer);
    }
}
