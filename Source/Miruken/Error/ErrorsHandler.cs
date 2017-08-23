namespace Miruken.Error
{
    using System;
    using Callback;
    using Concurrency;
    using Infrastructure;

    public class ErrorsHandler : Handler, IErrors
    {
        public virtual Promise HandleException(
            Exception exception, object callback, object context)
        {
            Console.WriteLine(exception);
            return Promise.Rejected(new RejectedException(callback));
        }
    }

    public static class ErrorsExtensions
    {
        public static IHandler Recover(this IHandler handler)
        {
            return Recover(handler, null);
        }

        public static IHandler Recover(this IHandler handler, object context)
        {
            return handler.Filter((callback, composer, proceed) => {
                if (callback is Composition)
                    return proceed();

                var cb = callback as ICallback;

                try
                {
                    var handled = proceed();
                    if (handled)
                    {
                        var promise = cb?.Result as Promise;
                        if (promise != null)
                        {
                            cb.Result = promise.Catch((ex, s) => 
                                ex is CancelledException
                                    ? Promise.Rejected(ex)
                                    : composer.Proxy<IErrors>().HandleException(ex, context))
                                    .Coerce(cb.ResultType);
                        }
                    }
                    return handled;
                }
                catch (Exception exception)
                {
                    if (cb?.ResultType?.Is<Promise>() == true)
                    {
                        cb.Result = (exception is CancelledException
                                    ? Promise.Rejected(exception)
                                    : composer.Proxy<IErrors>().HandleException(exception, context))
                                    .Coerce(cb.ResultType);
                        return true;
                    }
                    if (exception is CancelledException) return true;
                    composer.Proxy<IErrors>().HandleException(exception, context);
                    return true;
                }
            });
        }         
    }
}
