namespace Miruken.Callback
{
    using System.Threading;
    using Policy;

    public class SingletonLifestyle<T> : Lifestyle<T>
        where T : class
    {
        private T _instance;
        private bool _initialized;

        protected override bool GetInstance(MemberBinding member,
            Next<T> next, IHandler composer, out T instance)
        {
            if (_initialized)
            {
                instance = _instance;
            }
            else
            {
                object guard = this;
                instance = LazyInitializer.EnsureInitialized(
                    ref _instance, ref _initialized, ref guard,
                    () => next().GetAwaiter().GetResult());
            }
            return true;
        }
    }

    public class SingletonAttribute : LifestyleAttribute
    {
        public SingletonAttribute()
            : base(typeof(SingletonLifestyle<>))
        {          
        }
    }
}
