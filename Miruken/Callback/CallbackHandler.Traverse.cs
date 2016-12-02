using System;
using Miruken.Graph;

namespace Miruken.Callback
{
    public interface ICallbackHandlerAxis : ICallbackHandler
    {
        bool Handle(
            TraversingAxis axis, object callback, bool greedy, ICallbackHandler composer);
    }

    public class CallbackHandlerAxis : CallbackHandler, ICallbackHandlerAxis
    {
        private readonly ICallbackHandlerAxis _handler;
        private readonly TraversingAxis _axis;

        public CallbackHandlerAxis(ICallbackHandlerAxis handler, TraversingAxis axis)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");
            _handler = handler;
            _axis    = axis;
        }

        protected override bool HandleCallback(
            object callback, bool greedy, ICallbackHandler composer)
        {
            var composition = callback as Composition;
            var handled = composition == null
                 ? _handler.Handle(_axis, callback, greedy, composer)
                 : _handler.Handle(callback, greedy, composer);
            return handled || base.HandleCallback(callback, greedy, composer);
        }

        public bool Handle(
            TraversingAxis axis, object callback, bool greedy, ICallbackHandler composer)
        {
            return _handler.Handle(axis, callback, greedy, composer);
        }
    }

    public static class CallbackHandlerTraverseExtensions
    {
        public static ICallbackHandlerAxis Axis(
            this ICallbackHandlerAxis handler, TraversingAxis axis)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, axis);
        }

        public static ICallbackHandler Publish(this ICallbackHandlerAxis handler)
        {
            return handler.SelfDescendant().Notify();
        }

        #region Axis

        public static ICallbackHandlerAxis Self(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.Self);
        }

        public static ICallbackHandlerAxis Root(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.Root);
        }

        public static ICallbackHandlerAxis Child(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.Child);
        }

        public static ICallbackHandlerAxis Sibling(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.Sibling);
        }

        public static ICallbackHandlerAxis Ancestor(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.Ancestor);
        }

        public static ICallbackHandlerAxis Descendant(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.Descendant);
        }

        public static ICallbackHandlerAxis DescendantReverse(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.DescendantReverse);
        }

        public static ICallbackHandlerAxis SelfOrChild(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.SelfOrChild);
        }

        public static ICallbackHandlerAxis SelfOrSibling(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.SelfOrSibling);
        }

        public static ICallbackHandlerAxis SelfOrAncestor(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.SelfOrAncestor);
        }

        public static ICallbackHandlerAxis SelfDescendant(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.SelfOrDescendant);
        }

        public static ICallbackHandlerAxis SelfOrDescendantReverse(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.SelfOrDescendantReverse);
        }

        public static ICallbackHandlerAxis SelfSiblingOrAncestor(this ICallbackHandlerAxis handler)
        {
            return handler == null ? null 
                 : new CallbackHandlerAxis(handler, TraversingAxis.SelfSiblingOrAncestor);
        }

        #endregion    
    }   
}
