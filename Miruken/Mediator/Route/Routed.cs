namespace Miruken.Mediator.Route
{
    using System;
    using Callback;

    public abstract class Routed
    {
        public string Route { get; set; }
        public string Tag   { get; set; }
    }

    public class Routed<TResponse> : Routed,
        IRequestDecorator<TResponse>, IDecorator
    {
        public Routed()
        {
        }

        public Routed(IRequest<TResponse> request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Request = request;
        }

        public IRequest<TResponse> Request { get; set; }

        object IDecorator.Decoratee => Request;
    }

    public class RoutedRequest : Routed, IRequestDecorator, IDecorator
    {
        public RoutedRequest()
        {
        }

        public RoutedRequest(IRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Request = request;
        }

        public IRequest Request { get; set; }

        object IDecorator.Decoratee => Request;
    }

    public class RoutedNotification : Routed, INotificationDecorator, IDecorator
    {
        public RoutedNotification()
        {
        }

        public RoutedNotification(INotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            Notification = notification;
        }

        public INotification Notification { get; set; }

        object IDecorator.Decoratee => Notification;
    }
}
