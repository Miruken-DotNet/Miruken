namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Concurrency;
    using Policy;
    using Policy.Bindings;

    public class Initializer<T> : IFilter<Inquiry, T>
    {
        public int? Order { get; set; } = int.MaxValue - 1000;

        public async Task<T> Next(Inquiry callback,
            object rawCallback, MemberBinding member,
            IHandler composer, Next<T> next,
            IFilterProvider provider)
        {
            var result = await next();
            if (result is IInitialize initialize)
            {
                if (initialize.Initialized)
                    return result;
                try
                {
                    if (initialize.Initialize() is Promise promise)
                    {
                        await promise.Then((res, _) =>
                        {
                            initialize.Initialized = true;
                            return result;
                        }, (ex, _) =>
                        {
                            initialize.Initialized = false;
                            initialize.FailedInitialize(ex);
                            throw ex;
                        });
                    }
                }
                catch (Exception exception)
                {
                    initialize.Initialized = false;
                    initialize.FailedInitialize(exception);
                    throw;
                }
            }
            return result;
        }
    }

    public class InitializeProvider : IFilterProvider
    {
        public bool Required { get; } = true;

        public static readonly InitializeProvider Instance = new InitializeProvider();

        private InitializeProvider()
        {          
        }

        public IEnumerable<IFilter> GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            Type callbackType, IHandler composer)
        {
            var initializer = Initializers.GetOrAdd(dispatcher, d =>
            {
                var i = (IFilter)Activator.CreateInstance(
                    typeof(Initializer<>).MakeGenericType(d.LogicalReturnType));
                return i;
            });

            return new [] { initializer };
        }

        private static readonly ConcurrentDictionary<MemberDispatch, IFilter>
            Initializers = new ConcurrentDictionary<MemberDispatch, IFilter>();
    }
}
