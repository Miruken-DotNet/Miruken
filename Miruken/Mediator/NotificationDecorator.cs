namespace Miruken.Mediator
{
    using System;
    using Callback;

    public interface INotificationDecorator : INotification
    {
        INotification Notification { get; }
    }

    public class NotificationDecorator : INotificationDecorator, IDecorator
    {
        protected NotificationDecorator()
        {
        }

        protected NotificationDecorator(INotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            Notification = notification;
        }

        public INotification Notification { get; set; }

        object IDecorator.Decoratee => Notification;
    }
}
