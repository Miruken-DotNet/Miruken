namespace Miruken.Callback
{
    public class Batched<T>
    {
        public Batched(T callback, object rawCallback)
        {
            Callback    = callback;
            RawCallback = rawCallback;
        }

        public T      Callback   { get; }
        public object RawCallback { get; }
    }
}
