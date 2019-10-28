namespace Miruken.Callback
{
    using System.Threading.Tasks;
    using Policy.Bindings;

    public class SingletonLifestyle<T> : Lifestyle<T>
        where T : class
    {
        private volatile T _instance;

        protected override bool IsCompatibleWithParent(
            Inquiry parent, LifestyleAttribute attribute) => true;

        protected override Task<T> GetInstance(
            Inquiry inquiry, MemberBinding member,
            Next<T> next, IHandler composer,
            LifestyleAttribute attribute)
        {
            if (_instance == null)
            {
                lock (this)
                {
                    if (_instance == null)
                        _instance = next().GetAwaiter().GetResult();
                }
            }
            return _instance != null ? Task.FromResult(_instance) : null;
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
