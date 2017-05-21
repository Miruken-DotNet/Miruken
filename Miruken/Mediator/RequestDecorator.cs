namespace Miruken.Mediator
{
    using System;
    using Callback;

    public interface IRequestDecorator
    {
        IRequest Request { get; }
    }

    public interface IRequestDecorator<out TResponse>
    {
        IRequest<TResponse> Request { get; }
    }

    public abstract class RequestDecorator
        : IRequestDecorator, IRequest, IDecorator
    {
        protected RequestDecorator()
        {
        }

        protected RequestDecorator(IRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Request = request;
        }

        public IRequest Request { get; set; }

        object IDecorator.Decoratee => Request;
    }

    public abstract class RequestDecorator<TResponse>
        : IRequestDecorator<TResponse>, IRequest<TResponse>, IDecorator
    {
        protected RequestDecorator()
        {
        }

        protected RequestDecorator(IRequest<TResponse> request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Request = request;
        }

        public IRequest<TResponse> Request { get; set; }

        object IDecorator.Decoratee => Request;
    }
}
