namespace Miruken.Callback
{
    public delegate Res PipelineDelegate<out Res>(bool proceed = true);

    public interface IPieplineFilter<in Cb, Res>
    {
        Res Filter(Cb callback, IHandler composer, PipelineDelegate<Res> proceed);
    }
}
