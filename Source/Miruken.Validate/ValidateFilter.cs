namespace Miruken.Validate
{
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy.Bindings;
    using Concurrency;

    public class ValidateFilter<TCb, TRes> : IFilter<TCb, TRes>
    {
        [Provides, Singleton]
        public ValidateFilter()
        {
        }

        public int? Order { get; set; } = Stage.Validation;

        public Task<TRes> Next(TCb callback,
            object rawCallback, MemberBinding member,
            IHandler composer, Next<TRes> next,
            IFilterProvider provider)
        {
            var result = Validate(callback, composer)
                .Then((_, _) => next());
            return provider is ValidateAttribute { ValidateResult: true }
                 ? result.Then((resp, _) => Validate(resp, composer))
                 : result;
        }

        private static Promise<T> Validate<T>(T target, IHandler handler)
        {
            return target == null ? Promise<T>.Empty
                : handler.ValidateAsync(target).Then((outcome, _) =>
                {
                    if (!outcome.IsValid)
                        throw new ValidationException(outcome);
                    return target;
                });
        }
    }
}
