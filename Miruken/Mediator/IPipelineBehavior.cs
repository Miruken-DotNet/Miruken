namespace Miruken.Mediator
{
    using Callback;
    using Concurrency;

    public interface IPipelineBehavior<in TRequest, TResponse>
        : IFilter<TRequest, Promise<TResponse>>
    {
    }
}
