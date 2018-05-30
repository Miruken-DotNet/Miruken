namespace Miruken.Validate
{
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Concurrency;

    public class ValidateFilter<TCb, TRes> : IFilter<TCb, TRes>
    {
        public int? Order { get; set; } = Stage.Validation;

        public Task<TRes> Next(TCb callback, MethodBinding method,
            IHandler composer, Next<TRes> next,
            IFilterProvider provider = null)
        {
            var validator = composer.Proxy<IValidating>();
            return Validate(callback, validator)
                .Then((req, s) => next())
                .Then((resp, s) => Validate(resp, validator));
        }

        private static Promise<T> Validate<T>(T message, IValidating validator)
        {
            return message == null ? Promise<T>.Empty
                : validator.ValidateAsync(message).Then((outcome, s) =>
                {
                    if (!outcome.IsValid)
                        throw new ValidationException(outcome);
                    return message;
                });
        }
    }
}
