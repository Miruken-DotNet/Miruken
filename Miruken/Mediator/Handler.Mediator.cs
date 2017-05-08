namespace Miruken.Mediator
{
    using System;
    using Callback;
    using Concurrency;

    public static class HandlerMediatorExtensions
    {
        public static void Send(this IHandler handler, IRequest request)
        {
            if (handler == null) return;
            var req = new Request(request)
            {
                Policy = MediatesAttribute.Policy
            };
            if (!handler.Handle(req))
                throw new NotSupportedException($"{request.GetType()} not handled");
            if (req.IsAsync) ((Promise)req.Result).Wait();
        }

        public static Promise SendAsync(this IHandler handler, IRequest request)
        {
            if (handler == null)
                return Promise.Empty;
            var req = new Request(request)
            {
                Policy = MediatesAttribute.Policy
            };
            if (!handler.Handle(req))
                return Promise.Rejected(new NotSupportedException(
                    $"{request.GetType()} not handled"));
            return req.IsAsync ? (Promise) req.Result : Promise.Empty;
        }

        public static Resp Send<Resp>(this IHandler handler, IRequest<Resp> request)
        {
            if (handler == null)
                return default(Resp);
            var req = new Request(request)
            {
                Policy = MediatesAttribute.Policy
            };
            if (!handler.Handle(req))
                throw new NotSupportedException($"{request.GetType()} not handled");
            var result = req.Result;
            return req.IsAsync
                 ? (Resp)((Promise)result).Coerce(typeof(Promise<Resp>)).Wait()
                 : (Resp)result;
        }

        public static Promise<Resp> SendAsync<Resp>(
            this IHandler handler, IRequest<Resp> request)
        {
            if (handler == null)
                return Promise<Resp>.Empty;
            var req = new Request(request)
            {
                Policy = MediatesAttribute.Policy
            };
            if (!handler.Handle(req))
                return Promise<Resp>.Rejected(new NotSupportedException(
                    $"{request.GetType()} not handled"));
            var result  = req.Result;
            var promise = req.IsAsync
                        ? (Promise)result
                        : Promise.Resolved(result);
            return (Promise<Resp>)promise.Coerce(typeof(Promise<Resp>));
        }

        public static void Publish(this IHandler handler, INotification notification)
        {
            if (handler == null) return;
            var req = new Request(notification, true)
            {
                Policy = MediatesAttribute.Policy
            };
            if (handler.Handle(req, true) && req.IsAsync)
                ((Promise)req.Result).Wait();
        }

        public static Promise PublishAsync(this IHandler handler, INotification notification)
        {
            if (handler == null)
                return Promise.Empty;
            var req = new Request(notification, true)
            {
                Policy = MediatesAttribute.Policy
            };
            return handler.Handle(req, true) && req.IsAsync
                 ? (Promise)req.Result
                 : Promise.Empty;
        }
    }
}
