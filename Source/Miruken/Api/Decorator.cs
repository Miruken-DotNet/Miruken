namespace Miruken.Api
{
    using System;

    public class MessageDecorator : IDecorator
    {
        public MessageDecorator()
        {
        }

        public MessageDecorator(object message)
        {
            Message = message
                   ?? throw new ArgumentNullException(nameof(message));
        }

        public object Message { get; set; }

        object IDecorator.Decoratee => Message;
    }

    public class RequestDecorator<TResponse> : IRequest<TResponse>, IDecorator
    {
        public RequestDecorator()
        {
        }

        public RequestDecorator(IRequest<TResponse> request)
        {
            Request = request
                ?? throw new ArgumentNullException(nameof(request));
        }

        public IRequest<TResponse> Request { get; set; }

        object IDecorator.Decoratee => Request;
    }
}
