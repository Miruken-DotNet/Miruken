namespace Miruken.Callback
{
    using System.Threading;
    using System.Threading.Tasks;
    using Policy.Bindings;

    public class SingletonLifestyle<T> : Lifestyle<T>
        where T : class
    {
        private Task<T> _instance;
        private bool _initialized;

        protected override bool IsCompatibleWithParent(Inquiry parent) => true;

        protected override Task<T> GetInstance(
            Inquiry inquiry, MemberBinding member,
            Next<T> next, IHandler composer)
        {
            if (_initialized)
                return _instance;

            object guard = this;
            return LazyInitializer.EnsureInitialized(
                ref _instance, ref _initialized, ref guard,
                () => _instance = next());
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
