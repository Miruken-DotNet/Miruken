namespace Miruken.Register
{
    using Callback;

    [Unmanaged]
    public class InstanceProvider<T> : Handler
    {
        private readonly T _instance;

        public InstanceProvider(T instance)
        {
            _instance = instance;
        }

        [Provides, SkipFilters]
        public T Create(IHandler composer) => _instance;
    }
}
