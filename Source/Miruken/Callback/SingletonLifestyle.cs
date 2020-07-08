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
                    {
                        var result = next();
                        switch (result.Status)
                        {
                            case TaskStatus.RanToCompletion:
                                _instance = result.Result;
                                return result;
                            case TaskStatus.Faulted:
                            case TaskStatus.Canceled:
                                return result;
                            default:
                                _instance = result.GetAwaiter().GetResult();
                                break;
                        }
                    }
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
