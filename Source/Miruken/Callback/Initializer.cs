namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Policy;
    using Policy.Bindings;

    public class Initializer<TCb, TRes> : IFilter<TCb, TRes>
    {
        public int? Order { get; set; } = int.MaxValue - 100;

        public async Task<TRes> Next(TCb callback,
            object rawCallback, MemberBinding member,
            IHandler composer, Next<TRes> next,
            IFilterProvider provider)
        {
            var result = await next().ConfigureAwait(false);
            if (result is not IInitialize initialize) return result;
            if (initialize.Initialize() is { } promise)
                await promise;
            return result;
        }
    }

    public class InitializeProvider : IFilterProvider
    {
        public static readonly InitializeProvider Instance = new InitializeProvider();

        private InitializeProvider()
        {          
        }

        public bool Required { get; } = true;

        public bool? AppliesTo(object callback, Type callbackType)
        {
            return callback is Inquiry || callback is Creation;
        }

        public IEnumerable<IFilter> GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            object callback, Type callbackType, IHandler composer)
        {
            var initializer = Initializers.GetOrAdd(dispatcher,
                d => (IFilter)Activator.CreateInstance(
                    typeof(Initializer<,>).MakeGenericType(
                        callbackType, d.LogicalReturnType)));
            return new [] { initializer };
        }

        private static readonly ConcurrentDictionary<MemberDispatch, IFilter>
            Initializers = new ConcurrentDictionary<MemberDispatch, IFilter>();
    }
}
