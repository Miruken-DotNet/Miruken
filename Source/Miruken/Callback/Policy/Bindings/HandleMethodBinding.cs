namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class HandleMethodBinding : MemberBinding
    {
        private static readonly List<IFilterProvider> GlobalFilterList = new();

        public HandleMethodBinding(MethodDispatch dispatch)
             : base(dispatch)
        {
        }

        public static IEnumerable<IFilterProvider> GlobalFilters => GlobalFilterList;

        public static void AddGlobalFilters(params IFilterProvider[] providers)
        {
            if (providers == null || providers.Length == 0) return;
            GlobalFilterList.AddRange(providers.Where(p => p != null));
        }

        public bool Dispatch(object target, object callback, IHandler composer)
        {
            var oldComposer = Composer;
            var oldUnhandled = Unhandled;
            var handleMethod = (HandleMethod)callback;

            try
            {
                Composer = composer;
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
                Composer = oldComposer;
            }
        }

        private bool Invoke(HandleMethod handleMethod, object target, IHandler composer)
        {
            var arguments = handleMethod.Arguments;
            var filters   = composer.GetOrderedFilters(
                this, Dispatcher, handleMethod, typeof(HandleMethod),
                Filters, GlobalFilters);

            if (filters == null) return false;

            bool handled;
            object returnValue;

            if (filters.Count == 0)
            {
                returnValue = Dispatcher.Invoke(target, arguments);
                handled = !Unhandled;
            }
            else
            {
                handled = Dispatcher.GetPipeline(typeof(HandleMethod))
                    .Invoke(this, target, handleMethod, handleMethod,
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
                var method = Dispatcher.Member;
                return $"{method.ReflectedType?.FullName} | {method}";

            }
        }

        [ThreadStatic] internal static IHandler Composer;
        [ThreadStatic] internal static bool Unhandled;
    }
}
