namespace Miruken.Callback
{
    using System;
    using Concurrency;

    public static class HandlerRequestExtensions
    {
        public static void Request(this IHandler handler, object callback)
        {
            if (handler == null) return;
            if (!handler.Handle(new Request(callback)))
                throw new NotSupportedException($"{callback.GetType()} not handled");
        }

        public static Promise RequestAsync(this IHandler handler, object callback)
        {
            if (handler == null) return Promise.Empty;
            var request = new Request(callback);
            if (handler.Handle(request))
            {
                var result = request.Result;
                return request.IsAsync
                     ? (Promise)result
                     : Promise.Resolved(result);
            }
            return Promise.Rejected(new NotSupportedException(
                $"{callback.GetType()} not handled"));
        }

        public static Resp Request<Resp>(this IHandler handler, object callback)
        {
            if (handler == null) return default(Resp);
            var request = new Request(callback);
            if (handler.Handle(request))
            {
                var result = request.Result;
                return request.IsAsync
                     ? (Resp)((Promise)result).Wait()
                     : (Resp)result;
            }
            throw new NotSupportedException($"{callback.GetType()} not handled");
        }

        public static Promise<Resp> RequestAsync<Resp>(
            this IHandler handler, object callback)
        {
            if (handler == null)
                return Promise<Resp>.Empty;
            var request = new Request(callback);
            if (handler.Handle(request))
            {
                var result  = request.Result;
                var promise = request.IsAsync
                            ? (Promise)result
                            : Promise.Resolved(result);
                return (Promise<Resp>)promise.Coerce(typeof(Promise<Resp>));
            }
            throw new NotSupportedException($"{callback.GetType()} not handled");
        }
    }
}
