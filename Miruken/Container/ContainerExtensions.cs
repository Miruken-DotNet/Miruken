using SixFlags.CF.Miruken.Callback;

namespace SixFlags.CF.Miruken.Container
{
    public static class ContainerExtensions
    {
        #region Add Handlers

        public static ICompositeCallbackHandler AddHandler<T>(
            this ICompositeCallbackHandler handler)
        {
            var container  = new IContainer(handler);
            return handler.AddHandlers(container.Resolve<T>());
        }

        public static ICompositeCallbackHandler AddHandlers<T1, T2>(
            this ICompositeCallbackHandler handler)
        {
            var container = new IContainer(handler);
            return handler.AddHandlers(container.Resolve<T1>(),
                                       container.Resolve<T2>());
        }

        public static ICompositeCallbackHandler AddHandlers<T1, T2, T3>(
             this ICompositeCallbackHandler handler)
        {
            var container = new IContainer(handler);
            return handler.AddHandlers(container.Resolve<T1>(),
                                       container.Resolve<T2>(),
                                       container.Resolve<T3>());
        }

        public static ICompositeCallbackHandler AddHandlers<T1, T2, T3, T4>(
               this ICompositeCallbackHandler handler)
        {
            var container = new IContainer(handler);
            return handler.AddHandlers(container.Resolve<T1>(),
                                       container.Resolve<T2>(),
                                       container.Resolve<T3>(),
                                       container.Resolve<T4>());
        }

        #endregion

        #region Insert Handlers

        public static ICompositeCallbackHandler InsertHandler<T>(
            this ICompositeCallbackHandler handler, int atIndex)
        {
            var container = new IContainer(handler);
            return handler.InsertHandlers(atIndex, container.Resolve<T>());
        }


        public static ICompositeCallbackHandler InsertHandlers<T1, T2>(
            this ICompositeCallbackHandler handler, int atIndex)
        {
            var container = new IContainer(handler);
            return handler.InsertHandlers(atIndex, container.Resolve<T1>(),
                                                   container.Resolve<T2>());
        }

        public static ICompositeCallbackHandler InsertHandlers<T1, T2, T3>(
             this ICompositeCallbackHandler handler, int atIndex)
        {
            var container = new IContainer(handler);
            return handler.InsertHandlers(atIndex, container.Resolve<T1>(),
                                                   container.Resolve<T2>(),
                                                   container.Resolve<T3>());
        }

        public static ICompositeCallbackHandler InsertHandlers<T1, T2, T3, T4>(
               this ICompositeCallbackHandler handler, int atIndex)
        {
            var container = new IContainer(handler);
            return handler.InsertHandlers(atIndex, container.Resolve<T1>(),
                                                   container.Resolve<T2>(),
                                                   container.Resolve<T3>(),
                                                   container.Resolve<T4>());
        }

        #endregion
    }
}
