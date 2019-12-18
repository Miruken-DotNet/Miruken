namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Infrastructure;
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
                                break;
                            case TaskStatus.Faulted:
                            case TaskStatus.Canceled:
                                return result;
                            default:
                                _instance = next().GetAwaiter().GetResult();
                                break;
                        }
                    }
                    if (_instance is IDisposable disposable)
                        SingletonTracker.Add(disposable);
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
    
    internal static class SingletonTracker
    {
        private static ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();

        public static IDisposable Disposable => new DisposableAction(Dispose);

        internal static bool Track { get; set; }

        internal static void Add(IDisposable disposable)
        {
            if (Track)
                _disposables.Add(disposable);
        }

        internal static void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                try
                {
                   disposable.Dispose();
                }
                catch
                {
                    // ignored
                }
            }
            _disposables = new ConcurrentBag<IDisposable>();
        }
    }
}
