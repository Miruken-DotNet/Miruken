namespace Miruken.Register
{
    using Callback;

    public class InstanceProvider<T> : Handler
    {
        private readonly T _instance;

        public InstanceProvider(T instance)
        {
            _instance = instance;
        }

        [Provides]
        public T Create(IHandler composer) => _instance;
    }
}
