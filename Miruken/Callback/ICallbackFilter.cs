namespace Miruken.Callback
{
    public delegate Res CallbackDelegate<out Res>(bool proceed = true);

    public interface ICallbackFilter<in Cb, Res>
    {
        Res Filter(Cb callback, IHandler composer, CallbackDelegate<Res> proceed);
    }
}
