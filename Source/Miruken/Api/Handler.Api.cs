namespace Miruken.Api
{
    using System;
    using Callback;
    using Concurrency;

    public static class HandlerApiExtensions
    {
        public static Promise Send(this IHandler handler, object request)
        {
            if (handler == null)
                return Promise.Empty;
            var command = new Command(request)
            {
                WantsAsync = true,
            };
            try
            {
                return (new Stash() + handler.Infer()).Handle(command)
                     ? (Promise)command.Result
                     : Promise.Rejected(new NotSupportedException(
                          $"{request.GetType()} not handled"));
            }
            catch (Exception ex)
            {
                return Promise.Rejected(ex);
            }
        }

        public static Promise<TResp> Send<TResp>(this IHandler handler, object request)
        {
            if (handler == null)
                return Promise<TResp>.Empty;
            var command = new Command(request)
            {
                WantsAsync = true,
            };
            try
            {
                return (new Stash() + handler.Infer()).Handle(command)
                     ? (Promise<TResp>)((Promise)command.Result)
                           .Coerce(typeof(Promise<TResp>))
                     : Promise<TResp>.Rejected(new NotSupportedException(
                           $"{request.GetType()} not handled"));
            }
            catch (Exception ex)
            {
                return Promise<TResp>.Rejected(ex);
            }
        }

        public static Promise<TResp> Send<TResp>(this IHandler handler, IRequest<TResp> request)
        {
            if (handler == null)
                return Promise<TResp>.Empty;
            var command = new Command(request)
            {
                WantsAsync = true,
            };
            try
            {
                if (!(new Stash() + handler.Infer()).Handle(command))
                    return Promise<TResp>.Rejected(new NotSupportedException(
                        $"{request.GetType()} not handled"));
                var promise = (Promise)command.Result;
                return (Promise<TResp>)promise.Coerce(typeof(Promise<TResp>));
            }
            catch (Exception ex)
            {
                return Promise<TResp>.Rejected(ex);
            }
        }

        public static Promise Publish(this IHandler handler, object notification)
        {
            if (handler == null)
                return Promise.Empty;
            var command = new Command(notification, true)
            {
                WantsAsync = true,
            };
            try
            {
                return (new Stash() + handler.Infer()).Handle(command, true)
                     ? (Promise)command.Result
                     : Promise.Empty;
            }
            catch (Exception ex)
            {
                return Promise.Rejected(ex);
            }
        }
    }
}
