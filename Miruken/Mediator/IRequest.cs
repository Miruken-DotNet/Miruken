namespace Miruken.Mediator
{
    public interface IRequest { }

    public interface IRequest<out TResponse> { }
}
