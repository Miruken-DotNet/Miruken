using Miruken.Callback;

namespace Miruken.Container
{
    public static class ContainerExtensions
    {
        #region Add Handlers

        public static ICompositeHandler AddHandler<T>(
            this ICompositeHandler handler)
        {
            var container  = new IContainer(handler);
            return handler.AddHandlers(container.Resolve<T>());
        }

        public static ICompositeHandler AddHandlers<T1, T2>(
            this ICompositeHandler handler)
        {
            var container = new IContainer(handler);
            return handler.AddHandlers(container.Resolve<T1>(),
                                       container.Resolve<T2>());
        }

        public static ICompositeHandler AddHandlers<T1, T2, T3>(
             this ICompositeHandler handler)
        {
            var container = new IContainer(handler);
            return handler.AddHandlers(container.Resolve<T1>(),
                                       container.Resolve<T2>(),
                                       container.Resolve<T3>());
        }

        public static ICompositeHandler AddHandlers<T1, T2, T3, T4>(
               this ICompositeHandler handler)
        {
            var container = new IContainer(handler);
            return handler.AddHandlers(container.Resolve<T1>(),
                                       container.Resolve<T2>(),
                                       container.Resolve<T3>(),
                                       container.Resolve<T4>());
        }

        #endregion

        #region Insert Handlers

        public static ICompositeHandler InsertHandler<T>(
            this ICompositeHandler handler, int atIndex)
        {
            var container = new IContainer(handler);
            return handler.InsertHandlers(atIndex, container.Resolve<T>());
        }


        public static ICompositeHandler InsertHandlers<T1, T2>(
            this ICompositeHandler handler, int atIndex)
        {
            var container = new IContainer(handler);
            return handler.InsertHandlers(atIndex, container.Resolve<T1>(),
                                                   container.Resolve<T2>());
        }

        public static ICompositeHandler InsertHandlers<T1, T2, T3>(
             this ICompositeHandler handler, int atIndex)
        {
            var container = new IContainer(handler);
            return handler.InsertHandlers(atIndex, container.Resolve<T1>(),
                                                   container.Resolve<T2>(),
                                                   container.Resolve<T3>());
        }

        public static ICompositeHandler InsertHandlers<T1, T2, T3, T4>(
               this ICompositeHandler handler, int atIndex)
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
