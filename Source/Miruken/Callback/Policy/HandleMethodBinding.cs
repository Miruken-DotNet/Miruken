namespace Miruken.Callback.Policy
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class HandleMethodBinding : MethodBinding
    {
        public HandleMethodBinding(MethodDispatch dispatch)
             : base(dispatch)
        {       
        }

        public override bool Dispatch(object target, object callback,
            IHandler composer, ResultsDelegate results = null)
        {
            var oldComposer  = Composer;
            var oldUnhandled = Unhandled;
            var handleMethod = (HandleMethod)callback;

            try
            {
                Composer  = composer;
                Unhandled = false;
                return Invoke(handleMethod, target, composer);
            }
            catch (Exception exception)
            {
                if (exception is TargetException tie)
                    exception = tie.InnerException;
                handleMethod.Exception = exception;
                throw;
            }
            finally
            {
                Unhandled = oldUnhandled;
                Composer  = oldComposer;
            }
        }

        private bool Invoke(
            HandleMethod handleMethod, object target, IHandler composer)
        {
            var arguments = handleMethod.Arguments;
            var filters   = composer.GetOrderedFilters(
                this, typeof(HandleMethod), typeof(object),
                Filters, Dispatcher.Owner.Filters).ToArray();

            bool handled;
            object returnValue;

            if (filters.Length == 0)
            {
                returnValue = Dispatcher.Invoke(target, arguments);
                handled     = !Unhandled;
            }
            else
            {
                handled = Pipeline.Invoke(this, target, handleMethod, 
                    (IHandler comp, out bool completed) =>
                    {
                        completed = true;
                        if (comp != null && !ReferenceEquals(composer, comp))
                            Composer = comp;
                        return Dispatcher.Invoke(target, arguments);
                    },
                    composer, filters, out returnValue) && !Unhandled;
            }

            handleMethod.ReturnValue = handled 
                   ? CoerceResult(returnValue, handleMethod.Method.ReturnType)
                   : RuntimeHelper.GetDefault(handleMethod.ResultType);
            return handled;
        }

        private string DebuggerDisplay
        {
            get
            {
                var method = Dispatcher.Method;
                return $"{method.ReflectedType?.FullName} | {method}";

            }
        }

        private static readonly MethodPipeline Pipeline = MethodPipeline
            .GetPipeline(typeof(HandleMethod), typeof(object));

        [ThreadStatic] internal static IHandler Composer;
        [ThreadStatic] internal static bool     Unhandled;
    }
}
