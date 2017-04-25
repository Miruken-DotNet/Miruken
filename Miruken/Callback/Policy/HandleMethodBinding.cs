namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Concurrency;
    using Infrastructure;

    public class HandleMethodBinding : MethodBinding
    {
        public HandleMethodBinding(MethodDispatch dispatch)
             : base(dispatch)
        {       
        }

        public override bool Dispatch(
            object target, object callback, IHandler composer)
        {
            var handleMethod = (HandleMethod)callback;
            var resultType   = handleMethod.ResultType;

            var oldComposer  = Composer;
            var oldUnhandled = Unhandled;

            try
            {
                Composer  = composer;
                Unhandled = false;
                handleMethod.ReturnValue = Invoke(handleMethod, target, composer);
                return !Unhandled;
            }
            catch (Exception exception)
            {
                var tie = exception as TargetException;
                if (tie != null) exception = tie.InnerException;
                handleMethod.Exception = exception;
                if (!typeof(Promise).IsAssignableFrom(resultType)) throw;
                handleMethod.ReturnValue = 
                    Promise.Rejected(exception).Coerce(resultType);
                return true;
            }
            finally
            {
                Unhandled = oldUnhandled;
                Composer  = oldComposer;
            }
        }

        private object Invoke(
            HandleMethod handleMethod, object target, IHandler composer)
        {
            var method    = Dispatcher.Method;
            var arguments = handleMethod.Arguments;
            var options   = composer.GetFilterOptions();
            var filters   = composer.GetOrderedFilters(options,
                    FilterAttribute.GetFilters(target.GetType(), true),
                    FilterAttribute.GetFilters(method))
                .OfType<IFilter<HandleMethod, object>>()
                .ToArray();

            if (filters.Length == 0)
                return Dispatcher.Invoke(target, arguments);

            object returnValue;
            if (!Pipeline.Invoke(this, target, handleMethod,
                () => Dispatcher.Invoke(target, arguments),
                composer, filters, out returnValue))
                returnValue = RuntimeHelper.GetDefault(handleMethod.ResultType);
            handleMethod.ReturnValue = returnValue;
            return returnValue;
        }

        private static readonly MethodPipeline Pipeline =
            MethodPipeline.GetPipeline(typeof(HandleMethod), typeof(object));

        [ThreadStatic] internal static IHandler Composer;
        [ThreadStatic] internal static bool     Unhandled;
    }
}
